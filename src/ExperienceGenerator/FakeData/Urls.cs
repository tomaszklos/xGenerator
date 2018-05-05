using System;
using System.Collections.Generic;


namespace ExperienceGenerator.FakeData
{
    public class Urls
    {
        public static List<string> GetUrls()
        {
            List<string> result = new List<string>()
            {
                "https://doc.sitecore.net",
                "https://sitecore.stackexchange.com",
                "https://www.sitecore.com",
                "https://twitter.com/sitecore"
            };


            return result;
        }
    }
}
