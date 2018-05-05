using RandomNameGeneratorLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ExperienceGenerator.Location;
using static xConnectDataGenerator.XConnect.XConnectContact;
using static xConnectDataGenerator.XConnect.XConnectInteraction;

namespace ExperienceGenerator.FakeData
{
    public class GenerateRandomData
    {
        public static void GenarateFakeData(Dictionary<string, CultureInfo> countryCodesMapping, int geographicalLocationNumber, out Gender randomGender, out Devices randomDevice, out CultureInfo countryCode, out string firstName, out string middleName, out string nickname, out string lastName, out string email, out string url, out List<string> userAgents, out string city, out string ip, out string conName, out string conCode, out string regionName, out string regionCode, out float latitude, out float longitude, out string postalCode, out string metroCode, out string goalGuid, out string userA)
        {
            var values = Enum.GetValues(typeof(Gender));
            randomGender = (Gender)values.GetValue(new Random().Next(values.Length));
            var devices = Enum.GetValues(typeof(Devices));
            randomDevice = (Devices)devices.GetValue(new Random().Next(devices.Length));
            countryCode = countryCodesMapping.ElementAt(new Random().Next(0, countryCodesMapping.Count)).Value;
            firstName = randomGender.Equals(Gender.Female) ? new PersonNameGenerator().GenerateRandomFemaleFirstName() : new PersonNameGenerator().GenerateRandomFirstName();
            Thread.Sleep(100);
            middleName = randomGender.Equals(Gender.Female) ? new PersonNameGenerator().GenerateRandomFemaleFirstName() : new PersonNameGenerator().GenerateRandomFirstName();
            nickname = randomGender.Equals(Gender.Female) ? new PersonNameGenerator().GenerateRandomFemaleFirstName() : new PersonNameGenerator().GenerateRandomFirstName();
            lastName = new PersonNameGenerator().GenerateRandomLastName();
            email = firstName.ToLower() + "." + lastName.ToLower() + "@test.com";
            var urls = Urls.GetUrls();
            url = urls[new Random().Next(urls.Count)];
            var chanels = Chanels.GetChanels();
            var dev = FakeDevices.GetDevices();
            userAgents = UserAgents.GetUserAgents();
            var geoIndex = new Random().Next(1, geographicalLocationNumber);
            var cities = Cities.GetCities();
            city = cities[geoIndex];
            var ips = IPs.GetIPs();
            ip = ips[geoIndex];
            var countryNames = CountryNames.GetCountryNames();
            conName = countryNames[geoIndex];
            var countryCodes = CountryCodes.GetCountryCodes();
            conCode = countryCodes[geoIndex];
            var regionNames = RegionNames.GetRegionNames();
            regionName = regionNames[geoIndex];
            var regionCodes = RegionCodes.GetRegionCodes();
            regionCode = regionCodes[geoIndex];
            var latitudes = Latitudes.GetLatitudes();
            latitude = float.Parse(latitudes[geoIndex]);
            var longitudes = Longitudes.GetLongitudes();
            longitude = float.Parse(longitudes[geoIndex]);
            var postalCodes = PostalCodes.GetPostalCodes();
            postalCode = postalCodes[geoIndex];
            var metroCodes = MetroCodes.GetMetroCodes();
            metroCode = metroCodes[geoIndex];
            var profs = FakeProfiles.GetProfiles();
            var prof = profs[new Random().Next(profs.Count)];
            var goals = Goals.GetGoals();
            goalGuid = (string)goals[new Random().Next(goals.Count)];
            var outcomes = FakeOutcomes.GetOutcomes();
            //outcomeGuid = (string)outcomes[new Random().Next(outcomes.Count)];
            userA = userAgents[new Random().Next(userAgents.Count)];
            //chanelGuid = chanels[new Random().Next(chanels.Count)];
        }

        public static Dictionary<string, CultureInfo> GenerateCultures()
        {
            var countryCodesMapping = new Dictionary<string, CultureInfo>();

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            foreach (var culture in cultures)
            {

                countryCodesMapping.Add(Guid.NewGuid().ToString(), culture);

            }

            return countryCodesMapping;
        }
    }
}
