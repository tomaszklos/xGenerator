using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Colossus;
using System.Configuration;
using Sitecore.XConnect;
using Sitecore.Diagnostics;
using xConnectDataGenerator.XConnect;
using static xConnectDataGenerator.XConnect.XConnectContact;
using System.Globalization;
using static xConnectDataGenerator.XConnect.XConnectInteraction;
using ExperienceGenerator.FakeData;
using Sitecore.XConnect.Collection.Model;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator
{
    public abstract class XGenJobManager
    {
        public static XGenJobManager Instance { get; set; }


        protected XGenJobManager()
        {
            Threads = Environment.ProcessorCount*2;
        }

        private readonly ConcurrentDictionary<Guid, JobInfo> _jobs = new ConcurrentDictionary<Guid, JobInfo>();


        public int Threads { get; set; }
        public virtual TimeSpan WarmUpInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IEnumerable<JobInfo> Jobs => _jobs.Values.ToArray();

        public JobInfo StartNew(JobSpecification spec)
        {
            var info = new JobInfo(spec);

            info = _jobs.GetOrAdd(info.Id, id => info);

            //Try create a simulator to see if the spec contains any errors, to report them in the creating thread
            spec.CreateSimulator();

            var batchSize = (int) Math.Floor(info.Specification.VisitorCount/(double) Threads);

            for (var i = 0; i < Threads; i++)
            {
                var count = i == Threads - 1 ? info.Specification.VisitorCount - i*batchSize : batchSize;

                if (count <= 0)
                    continue;
                var segment = new JobSegment(info)
                              {
                                  TargetVisitors = count
                              };
                info.Segments.Add(segment);

                StartJob(info, segment);

                if (i == 0)
                {
                    // Fix for concurrent creating of Contacts.
                    Thread.Sleep(WarmUpInterval);
                }
            }

            return info;
        }

        public JobInfo Poll(Guid id)
        {
            JobInfo info;
            return _jobs.TryGetValue(id, out info) ? info : null;
        }


        protected abstract void StartJob(JobInfo info, JobSegment job);


        protected void Process(JobSegment job)
        {
            try
            {
                if (job.JobStatus <= JobStatus.Paused)
                {
                    job.Started = DateTime.Now;

                    if (job.JobStatus == JobStatus.Pending)
                    {
                        job.JobStatus = JobStatus.Running;
                    }
                    Randomness.Seed((job.Id.GetHashCode() + DateTime.Now.Ticks).GetHashCode());

                    var simulator = job.Specification.CreateSimulator();
                    var channels = job.Specification.Channels;
                    var channelGuid = channels.ElementAt(new Random().Next(0, channels.Count)).Key;
                    var outcomes = job.Specification.Outcomes;
                    var outcomeGuid = outcomes.Where(x=>x.Value > 0).ElementAt(new Random().Next(0, outcomes.Count)).Key;
                    var cities = job.Specification.Cities;

                    var xConnectUrl = ConfigurationManager.ConnectionStrings["xconnect.collection"].ConnectionString;

                    using (var client = XConnectClientCustom.GetClient(xConnectUrl))
                    {
                        try
                        {
                            Dictionary<string, CultureInfo> countryCodesMapping = GenerateRandomData.GenerateCultures();

                            foreach (var visitor in simulator.GetVisitors(job.TargetVisitors))
                            {
                                Thread.Sleep(100);

                                Gender randomGender;
                                Devices randomDevice;
                                CultureInfo countryCode;
                                string firstName, middleName, nickname, lastName, email, url, city, ip, conName, conCode, regionName, regionCode, postalCode, metroCode, goalGuid, userA;
                                List<string> userAgents;
                                float latitude, longitude;

                                GenerateRandomData.GenarateFakeData(countryCodesMapping, 100, out randomGender, out randomDevice, out countryCode, out firstName, out middleName, out nickname, out lastName, out email, out url, out userAgents, out city, out ip, out conName, out conCode, out regionName, out regionCode, out latitude, out longitude, out postalCode, out metroCode, out goalGuid, out userA);

                                var cityElement = cities[new Random().Next(cities.Count)];

                                var contact = CreateContact(
                                    60, 
                                    client,
                                    randomGender,
                                    countryCode,
                                    firstName,
                                    middleName,
                                    nickname,
                                    lastName,
                                    email,
                                    (cityElement!=null ? cityElement.Country.Name : conName),
                                    (cityElement != null ? cityElement.CountryCode : conCode),
                                    (cityElement != null ? cityElement.Country.Area.ToString() : regionName),
                                    (cityElement != null ? cityElement.RegionCode: regionCode),
                                    (cityElement != null ? cityElement.Name : city),
                                    (cityElement != null ? cityElement.TimeZone : postalCode),
                                    (cityElement != null ? cityElement.AsciiName : metroCode),
                                    (cityElement != null ? (float)cityElement.Latitude : latitude),
                                    (cityElement != null ? (float)cityElement.Longitude : longitude));


                                while (job.JobStatus == JobStatus.Paused)
                                {
                                    Thread.Sleep(100);
                                }

                                if (job.JobStatus > JobStatus.Paused)
                                {
                                    break;
                                }

                                try
                                {
                                    //Add ineraction                                  
                                    var interaction = CreateInteraction(client, userAgents, contact, channelGuid, ip, conName, conCode, regionName, regionCode, city, metroCode, postalCode, latitude, longitude, userA, url,
                                              randomDevice, countryCode,
                                              countryCodesMapping, lastName,
                                              goalGuid, outcomeGuid);

                                    var deviceProfile = XConnectDeviceProfile.CreateDeviceProfile(contact, interaction);

                                    foreach (var visit in visitor.Commit())
                                    {
                                        var urls = Urls.GetUrls();
                                        var pageViewUrl = urls[new Random().Next(urls.Count)];
                                        PageViewEvent pageView1 = XConnectInteraction.CreatePageViewEvent(Guid.NewGuid(), new Random().Next(1, 70), countryCode, lastName, pageViewUrl);
                                        interaction.Events.Add(pageView1);

                                        ++job.CompletedVisits;
                                    }
                                    client.AddDeviceProfile(deviceProfile);
                                    client.AddInteraction(interaction);
                                }
                                catch (Exception ex)
                                {
                                    ++job.Exceptions;
                                    job.LastException = ex.ToString();
                                }

                                client.AddContact(contact);
                                ++job.CompletedVisitors;
                            }

                            XConnectClientCustom.Submit(client);

                            Log.Info(" XConnectClientCustom.Submit", this);

                        }
                        catch (XdbExecutionException ex)
                        {
                            Log.Error(ex.Message, ex, ex.GetType());
                        }
                    }
                }


                job.Ended = DateTime.Now;
                job.JobStatus = job.CompletedVisitors < job.TargetVisitors ? JobStatus.Cancelled : JobStatus.Complete;
            }
            catch (Exception ex)
            {
                job.JobStatus = JobStatus.Failed;
                job.Ended = DateTime.Now;
                job.LastException = ex.ToString();
            }
        }
    }
}
