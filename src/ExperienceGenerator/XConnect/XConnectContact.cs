using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xConnectDataGenerator.XConnect
{

    public class XConnectContact
    {

        public static Contact CreateContact(int birthdateRange, XConnectClient client, Gender randomGender, CultureInfo cultureInfo, string firstName, string middleName, string nickname, string lastName, string email,
            string countryName, string countryCode, string regionName, string regionCode, string city, string postalCode, string metroCode, float latitude, float longitude)
        {
            Contact contact = new Contact(new ContactIdentifier(lastName, email, ContactIdentifierType.Known));

            PersonalInformation personalInfoFacet = CreatePersonalInformation(birthdateRange, randomGender, cultureInfo, firstName, middleName, nickname, lastName);
            client.SetFacet<PersonalInformation>(contact, PersonalInformation.DefaultFacetKey, personalInfoFacet);

            EmailAddressList emails = CreateEmailAddressList(email);
            client.SetFacet<EmailAddressList>(contact, EmailAddressList.DefaultFacetKey, emails);

            AddressList addresses = CreateAddressList( countryName,  countryCode,  regionName,  regionCode,  city,  postalCode,  metroCode,  latitude,  longitude);
            client.SetFacet<AddressList>(contact, AddressList.DefaultFacetKey, addresses);

            PhoneNumberList phoneNumberList = CreatePhoneNumberList(countryCode);
            client.SetFacet<PhoneNumberList>(contact, PhoneNumberList.DefaultFacetKey, phoneNumberList);

            return contact;
        }

        private static PersonalInformation CreatePersonalInformation(
            int birthdateRange,
            Gender randomGender,
            CultureInfo countryCode,
            string firstName,
            string middleName,
            string nickname,
            string lastName)
        {
            return new PersonalInformation()
            {
                FirstName = firstName,
                LastName = lastName,
                Nickname = nickname,
                MiddleName = middleName,
                Birthdate = DateTime.Today.AddDays(-new Random().Next(birthdateRange * 365)),
                Gender = randomGender.ToString(),
                Suffix = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 4),
                Title = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 8),
                PreferredLanguage = countryCode.TwoLetterISOLanguageName,
                JobTitle = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 8),

            };
        }
        private static PhoneNumberList CreatePhoneNumberList(string countryCode)
        {
            return new PhoneNumberList(
                                        new PhoneNumber(
                                            countryCode,
                                            DateTime.UtcNow.Ticks.ToString().Substring(8)),
                                        "Home");
        }

        private static EmailAddressList CreateEmailAddressList(string email)
        {
            return new EmailAddressList(
                                        new EmailAddress(email, true),
                                        "Home");
        }
        private static AddressList CreateAddressList(string countryName,string countryCode,string regionName,string regionCode,string city,string postalCode,string metroCode,float latitude,float longitude)
        {
            return new AddressList(
                                        new Address()
                                        {
                                            AddressLine1 = countryName,
                                            AddressLine2 = countryCode,
                                            AddressLine3 = regionName,
                                            AddressLine4 = regionCode,
                                            GeoCoordinate = new GeoCoordinate(latitude, longitude),
                                            CountryCode = countryCode,
                                            City = city,
                                            PostalCode = postalCode,
                                            StateOrProvince = metroCode,



                                        },
                                    "Home");
        }

        public static void GetContacts(string uri)
        {
            using (XConnectClient client = XConnectClientCustom.GetClient(uri))
            {
                try
                {
                    IAsyncQueryable<Contact> queryable = client.Contacts
                        .Where(c => c.GetFacet<PersonalInformation>(PersonalInformation.DefaultFacetKey).FirstName != string.Empty)
                        .WithExpandOptions(new ContactExpandOptions(PersonalInformation.DefaultFacetKey,
                                                                    EmailAddressList.DefaultFacetKey,
                                                                    AddressList.DefaultFacetKey,
                                                                    PhoneNumberList.DefaultFacetKey));

                    var enumerator = queryable.GetBatchEnumeratorSync();

                    while (enumerator.MoveNext())
                    {
                        foreach (var contact in enumerator.Current)
                        {
                            if (contact.Id.Equals("{99fbbf633ecb48b796853568d8180b05}"))
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

        public static void DeleteContacts(string uri)
        {
            using (XConnectClient client = XConnectClientCustom.GetClient(uri))
            {
                try
                {
                    IAsyncQueryable<Contact> queryable = client.Contacts
                        .Where(c => c.GetFacet<PersonalInformation>(PersonalInformation.DefaultFacetKey).FirstName != string.Empty)
                        .WithExpandOptions(new ContactExpandOptions(PersonalInformation.DefaultFacetKey,
                                                                    EmailAddressList.DefaultFacetKey,
                                                                    AddressList.DefaultFacetKey,
                                                                    PhoneNumberList.DefaultFacetKey));

                    var enumerator = queryable.GetBatchEnumeratorSync();

                    while (enumerator.MoveNext())
                    {
                        foreach (var contact in enumerator.Current)
                        {
                            client.ExecuteRightToBeForgotten(contact);
                            client.Submit();
                        }
                    }
                }
                catch (XdbExecutionException ex)
                {
                    // Handle exception or timeout
                }
            }
        }
        public enum Gender
        {
            Male,
            Female
        }
    }
}
