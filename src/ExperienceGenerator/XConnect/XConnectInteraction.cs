using ExperienceGenerator.FakeData;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xConnectDataGenerator.XConnect
{
    public class XConnectInteraction
    {
        public static Interaction CreateInteraction(XConnectClient client,List<string> userAgents, Contact contact, string chanelGuid,
            string ip,string conName,string conCode, string regionName, string regionCode, string city, string metroCode, string postalCode, float latitude, float longitude, string userA, string url,
            Devices randomDevice, CultureInfo countryCode,Dictionary<string,CultureInfo> countryCodesMapping,string lastName,
            string goalGuid,string outcomeGuid)
        {

            Interaction interaction= new Interaction(contact, InteractionInitiator.Contact, Guid.Parse(chanelGuid), userAgents[new Random().Next(userAgents.Count)]);

            var ipInfo = CreateIpInfo(ip, conName, conCode, regionName, regionCode, city, metroCode, postalCode, latitude, longitude, userA, url);
            client.SetFacet<IpInfo>(interaction, IpInfo.DefaultFacetKey, ipInfo);

            var userAgentInfo = CreateUserAgentInfo(randomDevice);
            client.SetFacet<UserAgentInfo>(interaction, UserAgentInfo.DefaultFacetKey, userAgentInfo);


            Goal goal = CreateGoal(goalGuid);
            interaction.Events.Add(goal);

            var outcome = CreateOutcome(outcomeGuid);
            interaction.Events.Add(outcome);

            var webVisitFacet = CreateWebVisit(countryCode, url);
            client.SetWebVisit(interaction, webVisitFacet);

            var localeInfo = XConnectInteraction.CreateLocalInfo(latitude, longitude);
            client.SetFacet<LocaleInfo>(interaction, LocaleInfo.DefaultFacetKey, localeInfo);

            return interaction;

        }
        private static IpInfo CreateIpInfo(string ip,
    string countryName,
    string countryCode,
    string regionName,
    string regionCode,
    string cityName,
    string metroCode,
    string postalCode,
    float latitude,
    float longitude,
    string userAgent,
    string url)//,string city,string ip)
        {
            var ipInfo = new IpInfo(ip);
            ipInfo.Isp = userAgent;
            ipInfo.Latitude = latitude;
            ipInfo.Longitude = longitude;
            ipInfo.MetroCode = metroCode;
            ipInfo.AreaCode = regionCode;
            ipInfo.BusinessName = "BusinessName : " + countryName + " , " + countryCode + " , " + cityName + " , " + postalCode;
            ipInfo.City = cityName;
            ipInfo.Country = countryCode;
            ipInfo.PostalCode = postalCode;
            ipInfo.Region = regionCode;
            ipInfo.Url = url;
            ipInfo.Dns = "dnsvalue";
            ipInfo.IpAddress = ip;

            return ipInfo;
        }
        private static UserAgentInfo CreateUserAgentInfo(Devices randomDevice)
        {
            var userAgentInfo = new UserAgentInfo();
            userAgentInfo.CanSupportTouchScreen = true;
            userAgentInfo.DeviceType = randomDevice.ToString();

            return userAgentInfo;
        }
        public static PageViewEvent CreatePageViewEvent(Guid itemId, int itemVersion, CultureInfo cultureInfo,string lastName,string url)
        {
            PageViewEvent pageView = new PageViewEvent(DateTime.Today.AddDays(-new Random().Next(1, 30)).ToUniversalTime(), itemId, itemVersion, cultureInfo.TwoLetterISOLanguageName);
            pageView.Duration = new TimeSpan(new Random().Next(1, 1000));
            pageView.Url = url;
            pageView.EngagementValue = new Random().Next(1, 20);
            pageView.ItemLanguage = cultureInfo.TwoLetterISOLanguageName;

            return pageView;
        }
        public static WebVisit CreateWebVisit(CultureInfo cultureInfo, string url)
        {
            var webVisitFacet = new WebVisit();
            webVisitFacet.Browser = new BrowserData() { BrowserMajorName = "Chrome", BrowserMinorName = "Desktop", BrowserVersion = "22.0" };
            webVisitFacet.Language = cultureInfo.TwoLetterISOLanguageName;
            webVisitFacet.OperatingSystem = new OperatingSystemData() { Name = "Windows", MajorVersion = "10", MinorVersion = "4", };
            webVisitFacet.Referrer = url;
            webVisitFacet.Screen = new ScreenData() { ScreenHeight = 1080, ScreenWidth = 685 };
            webVisitFacet.SearchKeywords = "sitecore";
            webVisitFacet.SiteName = "Home";


            return webVisitFacet;
        }

        public static Outcome CreateOutcome(string outcomeGuid)
        {
            var outcome = new Outcome(Guid.Parse(outcomeGuid), DateTime.UtcNow, "USD", 100.00m);
            return outcome;
        }

        public static LocaleInfo CreateLocalInfo(float latitude, float longitude)
        {
            LocaleInfo localeInfo = new LocaleInfo();
            localeInfo.TimeZoneOffset = new TimeSpan(new Random().Next(1, 200));
            localeInfo.GeoCoordinate = new GeoCoordinate(latitude, longitude);
            return localeInfo;
        }

        public static Goal CreateGoal(string goalGuid)
        {

            var goal = new Goal(Guid.Parse(goalGuid), DateTime.UtcNow);
            goal.Text = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 8);
            goal.EngagementValue = new Random().Next(1, 100);
            goal.Duration = new TimeSpan(new Random().Next(1, 3000));
            return goal;

        }

        public static ProfileScores CreateProfileScores(string prof)
        {
            ProfileScores profiles = new ProfileScores();
            ProfileScore score = new ProfileScore()
            {
                MatchedPatternId = Guid.NewGuid(),
                ProfileDefinitionId = Guid.Parse("B5BDEE45-C945-476F-9EE6-3B8A9255C17E"),
                Score = new Random().NextDouble(),
                ScoreCount = new Random().Next(1, 7),
                Values = new Dictionary<Guid, double>() {
                    { Guid.Parse(prof), new Random().NextDouble() } }
            };

            profiles.Scores.Add(Guid.Parse("B5BDEE45-C945-476F-9EE6-3B8A9255C17E"), score);

            return profiles;
        }

        public static void GetInteractions(string uri)
        {
            using (XConnectClient client = XConnectClientCustom.GetClient(uri))
            {
                try
                {
                    IAsyncQueryable<Interaction> queryable = client.Interactions
                        .Where(x => x.Events.OfType<Goal>().Any())
                        .WithExpandOptions(new InteractionExpandOptions(CollectionModel.FacetKeys.IpInfo,CollectionModel.FacetKeys.WebVisit));

                    var enumerable = queryable.GetBatchEnumeratorSync();

                    while (enumerable.MoveNext())
                    {
                        var interactionBatch = enumerable.Current; // Batch of <= 20 interactions

                        foreach (var interaction in interactionBatch)
                        {
                            if (interaction.Id.Equals("{99fbbf633ecb48b796853568d8180b05}"))
                            {

                            }
                        }
                    }
                }
                catch (XdbExecutionException ex)
                {
                    // Handle exception or timeout
                }
            }
        }
        public enum Devices
        {
            Default,
            Tablet,
            Mobile
        }
    }
}
