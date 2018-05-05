using ExperienceGenerator.FakeData;
using Sitecore.Diagnostics;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using Sitecore.XConnect.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static xConnectDataGenerator.XConnect.XConnectContact;
using static xConnectDataGenerator.XConnect.XConnectInteraction;

namespace xConnectDataGenerator.XConnect
{
    public class XConnectClientCustom
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static XConnectClient GetClient(string uri)
        {
            var config = new XConnectClientConfiguration(
                new XdbRuntimeModel(CollectionModel.Model),
                new Uri(uri),
                new Uri(uri));

            try
            {
                config.Initialize();
            }
            catch (XdbModelConflictException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return new XConnectClient(config);
        }
        public static void Submit(XConnectClient client)
        {
            try
            {
                client.SubmitAsync();
            }
            catch (XdbModelConflictException ex)
            {
                Log.Error(ex.Message,ex,ex.GetType());

                throw;
            }
        }
        public static void CreateContactWithInteraction(string uri, int contactNumber, int birthdateRange, Dictionary<string, CultureInfo> countryCodesMapping, int geographicalLocationNumber,string channel)
        {

            /*
            var contactNumber = 20;
            var birthdateRange = 60;
            var countryCodesMapping = GenerateRandomData.GenerateCultures();
            var geographicalLocationNumber = 100;
             */
            using (var client = GetClient(uri))
            {
                try
                {
                    for (int i = 1; i <= contactNumber; i++)
                    {
                        Thread.Sleep(100);

                        Gender randomGender;
                        Devices randomDevice;
                        CultureInfo countryCode;
                        string firstName, middleName, nickname, lastName, email, url, city, ip, conName, conCode, regionName, regionCode, postalCode, metroCode, goalGuid, outcomeGuid, userA;
                        List<string> userAgents;
                        float latitude, longitude;

                        var outcomes = FakeOutcomes.GetOutcomes();
                        outcomeGuid = (string)outcomes[new Random().Next(outcomes.Count)];

                        GenerateRandomData.GenarateFakeData(countryCodesMapping, geographicalLocationNumber, out randomGender, out randomDevice, out countryCode, out firstName, out middleName, out nickname, out lastName, out email, out url, out userAgents, out city, out ip, out conName, out conCode, out regionName, out regionCode, out latitude, out longitude, out postalCode, out metroCode, out goalGuid, out userA);

                        var contact = XConnectContact.CreateContact(birthdateRange, client, randomGender, countryCode, firstName, middleName, nickname, lastName, email, conName, conCode, regionName, regionCode, city, postalCode, metroCode, latitude, longitude);

                        var interaction = CreateInteraction(client, userAgents, contact, channel, ip, conName, conCode, regionName, regionCode, city, metroCode, postalCode, latitude, longitude, userA, url,
                                                  randomDevice, countryCode,
                                                  countryCodesMapping, lastName,
                                                  goalGuid, outcomeGuid);

                        var deviceProfile = XConnectDeviceProfile.CreateDeviceProfile(contact, interaction);

                        client.AddDeviceProfile(deviceProfile);
                        client.AddInteraction(interaction);
                        client.AddContact(contact);
                    }

                    client.Submit();

                }
                catch (XdbExecutionException ex)
                {
                    Log.Error(ex.Message, ex, ex.GetType());
                }
            }
        }
    }
}
