﻿using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using System.Xml;
using Colossus.Integration.Models;
using ExperienceGenerator.Client.Models;
using ExperienceGenerator.Client.Repositories;
using ExperienceGenerator.Data;
using ExperienceGenerator.Models;
using Newtonsoft.Json.Linq;
using Sitecore;
using Sitecore.Analytics.Aggregation;
using Sitecore.Analytics.Data.DataAccess.MongoDb;
using Sitecore.Analytics.Data.Items;
using Sitecore.Cintel.Configuration;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Sites;

namespace ExperienceGenerator.Client.Controllers
{
    public class ExperienceGeneratorActionsController : ApiController
    {
        private static SiteInfo[] GetSites(bool all)
        {
            var excludedSites = new HashSet<string>();

            var exportNode = Factory.GetConfigNode("experienceGenerator/excludeSites") as XmlElement;
            if (exportNode != null)
            {
                var sites = exportNode.SelectNodes("site");
                if (sites != null)
                {
                    foreach (var site in sites.OfType<XmlElement>())
                    {
                        excludedSites.Add(site.GetAttribute("name"));
                    }
                }
            }


            return SiteManager.GetSites().Select(s => new SiteContext(new Sitecore.Web.SiteInfo(s.Properties))).Select(site => new SiteInfo
                                                                                                                               {
                                                                                                                                   Id = site.Name,
                                                                                                                                   Host = StringUtil.GetString(site.TargetHostName.Split('|')[0], site.HostName.Split('|')[0]),
                                                                                                                                   StartPath = site.RootPath + site.StartItem,
                                                                                                                                   Label = site.Name,
                                                                                                                                   Database = site.Database != null ? site.Database.Name : ""
                                                                                                                               }).Where(site => all || !excludedSites.Contains(site.Id)).ToArray();
        }


        [HttpGet]
        public IEnumerable<SiteInfo> Websites(bool all = false)
        {
            return GetSites(all);
        }


        [HttpGet]
        public IEnumerable<Device> Devices()
        {
            return new DeviceRepository().GetAll();
        }

        [HttpGet]
        public IEnumerable<ItemInfo> Items(string query, int? maxDepth = null, string language = null)
        {
            var db = Database.GetDatabase("web");

            foreach (var item in db.SelectItems(query))
            {
                Language itemLanguage = null;
                if (!string.IsNullOrEmpty(language))
                {
                    Language.TryParse(language, out itemLanguage);
                }

                yield return ItemInfo.FromItem(item, GetSites(false).Select(w => w.Id), maxDepth, itemLanguage);
            }
        }


        [HttpGet]
        public ConfigurationOptions Options()
        {
            var options = new ConfigurationOptions();

            options.TrackerIsEnabled = !Settings.GetBoolSetting("ExperienceGenerator.DistributedEnvironment", false);

            options.Websites = GetSites(false).Select(s => new SelectionOption
                                                           {
                                                               Id = s.Id,
                                                               Label = s.Label,
                                                               DefaultWeight = s.Id == "website" ? 100 : 50
                                                           }).ToList();
            options.LocationGroups = Locations();

            var db = Database.GetDatabase("master");
            var channels = db.GetItem(KnownItems.ChannelsRoot);

            options.Version = "0.1";

            options.ChannelTypes = channels.Children.Where(x => x.TemplateID == ChanneltypeItem.TemplateID).Select(x => new OptionTypes
                                                                                                                        {
                                                                                                                            Type = x.DisplayName,
                                                                                                                            OptionGroups = SelectChannelGroups(x)
                                                                                                                        }).ToList();
            var outcomes = db.GetItem(KnownItems.OutcomesRoot);
            var taxonomyRoot = db.GetItem(KnownItems.TaxonomyRoot);

            var outcomeGroups = outcomes.Axes.GetDescendants().Where(c => c.TemplateID == OutcomeDefinitionItem.TemplateID).GroupBy(x => string.IsNullOrEmpty(x["Group"]) ? ID.Null : new ID(x["Group"]), x => x);

            options.OutcomeGroups = taxonomyRoot.Axes.GetDescendants().Where(c => c.TemplateID == OutcomegroupItem.TemplateID).Select(cg => new SelectionOptionGroup
                                                                                                                                            {
                                                                                                                                                Label = cg.Name,
                                                                                                                                                Options = outcomeGroups.Where(c => c.Key == cg.ID).SelectMany(x => x).Select(c => new SelectionOption
                                                                                                                                                                                                                                  {
                                                                                                                                                                                                                                      Id = c.ID.ToString(),
                                                                                                                                                                                                                                      Label = c.Name,
                                                                                                                                                                                                                                      DefaultWeight = 5
                                                                                                                                                                                                                                  }).OrderBy(item => item.Label).ToList()
                                                                                                                                            }).ToList();

            var outcomesWithoutGroup = outcomeGroups.Where(c => c.Key == ID.Null).SelectMany(x => x).Select(c => new SelectionOption
                                                                                                                 {
                                                                                                                     Id = c.ID.ToString(),
                                                                                                                     Label = c.Name,
                                                                                                                     DefaultWeight = 5
                                                                                                                 }).OrderBy(item => item.Label).ToList();
            if (outcomesWithoutGroup.Any())
            {
                options.OutcomeGroups.Add(new SelectionOptionGroup
                                          {
                                              Label = "None",
                                              Options = outcomesWithoutGroup
                                          });
            }

            var campaigns = db.GetItem(KnownItems.CampaignsRoot);
            options.Campaigns = campaigns.Axes.GetDescendants().Where(item => item.TemplateID == CampaignItem.TemplateID).Select(item => new SelectionOption
                                                                                                                                         {
                                                                                                                                             Id = item.ID.ToString(),
                                                                                                                                             Label = item.Paths.FullPath.Substring(campaigns.Paths.FullPath.Length + 1),
                                                                                                                                             DefaultWeight = 10
                                                                                                                                         }).OrderBy(e => e.Label).ToList();

            options.OrganicSearch = SearchEngine.SearchEngines.Where(e => !e.Ppc).Select(e => new SelectionOption
                                                                                              {
                                                                                                  Id = e.Id,
                                                                                                  Label = e.Label
                                                                                              }).ToList();

            options.PpcSearch = SearchEngine.SearchEngines.Where(e => e.Ppc).Select(e => new SelectionOption
                                                                                         {
                                                                                             Id = e.Id,
                                                                                             Label = e.Label
                                                                                         }).ToList();

            options.Languages = Context.Database.GetLanguages().Select(x => new SelectionOption
                                                                            {
                                                                                DefaultWeight = 50,
                                                                                Label = x.CultureInfo.DisplayName,
                                                                                Id = x.Name
                                                                            }).ToList();
            if (options.Languages.Count == 1)
            {
                options.Languages[0].DefaultWeight = 100;
            }

            return options;
        }

        [HttpGet]
        public List<SelectionOptionGroup> Locations()
        {
            return GeoRegion.Regions.Select(region => new SelectionOptionGroup
                                                      {
                                                          Label = region.Label,
                                                          Options = region.SubRegions.Select(x => new SelectionOption
                                                                                                  {
                                                                                                      Id = x.Id,
                                                                                                      Label = x.Label,
                                                                                                      DefaultWeight = 50
                                                                                                  })
                                                      }).ToList();
        }

        private static IEnumerable<SelectionOptionGroup> SelectChannelGroups(Item channelsRoot)
        {
            return channelsRoot.Children.Where(c => c.TemplateID == ChannelgroupItem.TemplateID).Select(cg => new SelectionOptionGroup
                                                                                                              {
                                                                                                                  Label = cg.Name,
                                                                                                                  Options = cg.Children.Where(c => c.TemplateID == ChannelItem.TemplateID).Select(c => new SelectionOption
                                                                                                                                                                                                       {
                                                                                                                                                                                                           Id = c.ID.ToString(),
                                                                                                                                                                                                           Label = c.Name,
                                                                                                                                                                                                           DefaultWeight = c.Name == "Direct" ? 50 : 0
                                                                                                                                                                                                       }).OrderBy(item => item.Label).ToList()
                                                                                                              }).ToList();
        }

        [HttpPost]
        public IHttpActionResult SaveSettings([FromBody] Preset preset)
        {
            var repo = new SettingsRepository();
            repo.Save(preset.Name, preset.Spec);
            return Json(new
                        {
                            message = "ok"
                        });
        }

        [HttpGet]
        public IHttpActionResult PresetQuery()
        {
            var repo = new SettingsRepository();
            return Json(new
                        {
                            query = repo.GetPresetsQuery()
                        });
        }

        [HttpGet]
        public JObject SettingsPreset(string id)
        {
            var repo = new SettingsRepository();
            return repo.GetSettingPreset(new ID(id));
        }

        [HttpGet]
        public JArray ContactSettingsPreset(string id)
        {
            var repo = new ContactSettingsRepository();
            return repo.GetContactSettingPreset(new ID(id));
        }

        [HttpPost]
        public IHttpActionResult SaveContactsSettings([FromBody] ContactPreset preset)
        {
            var repo = new ContactSettingsRepository();

            repo.Save(preset.Name, preset.Spec);
            return Json(new
                        {
                            message = "ok"
                        });
        }

        [HttpGet]
        public List<string> Presets()
        {
            var repo = new SettingsRepository();
            return repo.GetPresetsIds();
        }

        [HttpGet]
        public List<Country> Countries()
        {
            return new GeoDataRepository().GetCountries();
        }

        [HttpGet]
        public List<City> Cities(int id)
        {
            return new GeoDataRepository().GetCities(id);
        }


        [HttpPost]
        public IHttpActionResult StopAll()
        {
            foreach (var job in XGenJobManager.Instance.Jobs.Where(job => job.JobStatus <= JobStatus.Paused))
            {
                job.Stop();
            }

            return Ok();
        }

        [HttpPost]
        public IHttpActionResult Flush()
        {
            var driver = new MongoDbDriver(ConfigurationManager.ConnectionStrings["analytics"].ConnectionString);
            driver.ResetDatabase();

            var item = (Context.ContentDatabase ?? Context.Database).GetItem("/sitecore/media library/Images/xgen");
            item?.Delete();

            var sql = new SqlReportingStorageProvider("reporting");
            sql.ExcludedTableFromDataDeletion("dbo", "Segments");
            sql.DeleteAllReportingData();


            var index = ContentSearchManager.GetIndex(CustomerIntelligenceConfig.ContactSearch.SearchIndexName);
            index.Reset();
            index.Refresh();
            using (var ctx = index.CreateUpdateContext())
            {
                ctx.Optimize();
            }

            return Ok();
        }
    }

    public class GeoDataRepository
    {
        private static GeoData _cache;
        private static readonly object _lock = new object();

        private static GeoData Cache
        {
            get
            {
                lock (_lock)
                {
                    return _cache ?? (_cache = GeoData.FromResource());
                }
            }
        }

        public List<Country> GetCountries()
        {
            return Cache.Countries.Values.ToList();
        }

        public List<City> GetCities(int isoNumeric)
        {
            return Cache.CitiesByCountry[isoNumeric].ToList();
        }
    }
}