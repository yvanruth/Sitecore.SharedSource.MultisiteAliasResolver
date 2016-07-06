namespace Sitecore.SharedSource.MultisiteAliasResolver
{
    using System;
    using Sitecore.Pipelines.HttpRequest;
    using Sitecore.Data.Items;
    using Sitecore.Data.Fields;
    using Sitecore.Links;
    using Sitecore.Diagnostics;
    using Sitecore.Configuration;
    using Sitecore.Web;
    using Sitecore.Sites;
    using Sitecore;
    using Sitecore.IO;
    using System.IO;
    using System.Linq;

    class MultiSiteAliasResolver : AliasResolver
    {
        public new void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (!Settings.AliasesActive)
            {
                Tracer.Warning("Aliases are not active.");
            } 
            else 
            {
                Sitecore.Data.Database masterDb = Sitecore.Configuration.Factory.GetDatabase("master");
                if (masterDb == null)
                {
                    Tracer.Warning("There is no context database in AliasResover.");
                }
                else
                {
                    Item aliasItem = getAliasItem(args, masterDb);
                    if (aliasItem != null)
                    {
                        LinkField linkField = aliasItem.Fields["Linked item"];
                        if (linkField != null)
                        {
                            Item AliasLinkedTo = Sitecore.Context.Database.GetItem(linkField.TargetID);
                            if (AliasLinkedTo != null && AliasLinkedTo.Visualization.Layout != null)
                            {
                                Sitecore.Context.Item = AliasLinkedTo;

                                string aliasPath = AliasLinkedTo.Paths.FullPath;                               
                                SiteContext ctx = ResolveSiteContext(args, AliasLinkedTo);
                                if (ctx != null)
                                {
                                    Sitecore.Context.Site = ctx;
                                }
                            }
                        }
                    }
                }                
            }

            base.Process(args);
        }

        /// <summary>
        /// Resolves the site context.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        protected virtual SiteContext ResolveSiteContext(HttpRequestArgs args, Item aliasItem)
        {
            SiteContext siteContext;
            string queryString = WebUtil.GetQueryString("sc_site");
            if (queryString.Length > 0)
            {
                siteContext = SiteContextFactory.GetSiteContext(queryString);
                Assert.IsNotNull(siteContext, string.Concat("Site from query string was not found: ", queryString));
                return siteContext;
            }
            if (Settings.EnableSiteConfigFiles)
            {
                string[] directoryName = new string[] { Path.GetDirectoryName(args.Url.FilePath) };
                string str = StringUtil.GetString(directoryName);
                string str1 = FileUtil.MakePath(FileUtil.NormalizeWebPath(str), "site.config");
                if (FileUtil.Exists(str1))
                {
                    siteContext = SiteContextFactory.GetSiteContextFromFile(str1);
                    Assert.IsNotNull(siteContext, string.Concat("Site from site.config was not found: ", str1));
                    return siteContext;
                }
            }
            Uri requestUri = WebUtil.GetRequestUri();
            siteContext = SiteContextFactory.GetSiteContext(requestUri.Host, aliasItem.Paths.FullPath.Replace(Sitecore.Context.Site.RootPath, string.Empty), requestUri.Port);
            Assert.IsNotNull(siteContext, string.Concat("Site from host name and path was not found. Host: ", requestUri.Host, ", path: ", args.Url.FilePath));
            return siteContext;
        }

        /// <summary>
        /// Gets the alias item.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        private Item getAliasItem(HttpRequestArgs args, Sitecore.Data.Database masterDb) 
        {
            string websitePath = Sitecore.Context.Site.RootPath.ToLower();

            if (masterDb != null && Sitecore.Context.Site != null)
            {
                if (!string.IsNullOrEmpty(args.LocalPath) && args.LocalPath.Length > 1)
                {                    
                    Item aliasItem = masterDb.GetItem(string.Format("/{0}/{1}/{2}", websitePath, "setting", "aliases") + args.LocalPath);                   
                    if (aliasItem != null)
                    {                    
                        return aliasItem;
                    }
                }
            }            

            return null;
        }
    }
}

