namespace CommandTemplates.Classes
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

                                Uri requestUri = WebUtil.GetRequestUri();
                                Sitecore.Context.Site = SiteContextFactory.GetSiteContext(requestUri.Host, args.Url.FilePath, requestUri.Port);
                            }
                        }
                    }
                }

                
            }

            base.Process(args);
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
                    string folder = Sitecore.Context.Site.RootPath.ToLower().Contains("ggd") ? "/Settings" : "/Instellingen";
                    Item aliasItem = masterDb.GetItem(websitePath + folder + "/aliases" + args.LocalPath);

                    // Sitecore.Diagnostics.Log.Warn("Trying to fetch ALIAS for " + websitePath + folder + "/aliases" + args.LocalPath, this);

                    if (aliasItem != null)
                    {
                        // Sitecore.Diagnostics.Log.Warn("ALIAS found", this);
                        return aliasItem;
                    }
                    else
                    {
                        // Sitecore.Diagnostics.Log.Warn("ALIAS not found", this);
                    }
                }
            }            

            return null;
        }
    }
}

