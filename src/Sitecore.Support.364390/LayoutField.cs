namespace Sitecore.Support.Data.Fields
{
    using System.Collections;
    using System.Xml;
    using Diagnostics;
    using Layouts;
    using Links;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Pipelines;
    using Sitecore.Pipelines.ResolveRenderingDatasource;
    using Sitecore.Text;

    /// <summary>Represents a Layout field.</summary>
    public class LayoutField : Sitecore.Data.Fields.LayoutField
    {
        private readonly XmlDocument data;
        public LayoutField([NotNull] Item item)
            : base(item)
        {
        }
        public override void ValidateLinks([NotNull] LinksValidationResult result)
        {
            Assert.ArgumentNotNull(result, "result");

            string value = this.Value;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);

            ArrayList devices = layoutDefinition.Devices;
            if (devices == null)
            {
                return;
            }

            foreach (DeviceDefinition device in devices)
            {
                if (!string.IsNullOrEmpty(device.ID))
                {
                    Item deviceItem = this.InnerField.Database.GetItem(device.ID);

                    if (deviceItem != null)
                    {
                        result.AddValidLink(deviceItem, device.ID);
                    }
                    else
                    {
                        result.AddBrokenLink(device.ID);
                    }
                }

                if (!string.IsNullOrEmpty(device.Layout))
                {
                    Item layoutItem = this.InnerField.Database.GetItem(device.Layout);

                    if (layoutItem != null)
                    {
                        result.AddValidLink(layoutItem, device.Layout);
                    }
                    else
                    {
                        result.AddBrokenLink(device.Layout);
                    }
                }

                this.ValidatePlaceholderSettings(result, device);

                if (device.Renderings == null)
                {
                    continue;
                }

                foreach (RenderingDefinition rendering in device.Renderings)
                {
                    if (rendering.ItemID == null)
                    {
                        continue;
                    }

                    Item renderingItem = this.InnerField.Database.GetItem(rendering.ItemID);

                    if (renderingItem != null)
                    {
                        result.AddValidLink(renderingItem, rendering.ItemID);
                    }
                    else
                    {
                        result.AddBrokenLink(rendering.ItemID);
                    }

                    string datasource = rendering.Datasource;
                    if (!string.IsNullOrEmpty(datasource))
                    {
                        using (new ContextItemSwitcher(this.InnerField.Item))
                        {
                            var args = new ResolveRenderingDatasourceArgs(datasource);
                            CorePipeline.Run("resolveRenderingDatasource", args, false);
                            datasource = args.Datasource;
                        }

                        Item dataSourceItem = this.InnerField.Database.GetItem(datasource, Context.ContentLanguage);
                        if (dataSourceItem != null)
                        {
                            result.AddValidLink(dataSourceItem, datasource);
                        }
                        else
                        {
                            if (!datasource.Contains(":"))
                            {
                                result.AddBrokenLink(datasource);
                            }
                        }
                    }

                    string mvTest = rendering.MultiVariateTest;
                    if (!string.IsNullOrEmpty(mvTest))
                    {
                        Item testDefinitionItem = this.InnerField.Database.GetItem(mvTest);
                        if (testDefinitionItem != null)
                        {
                            result.AddValidLink(testDefinitionItem, mvTest);
                        }
                        else
                        {
                            result.AddBrokenLink(mvTest);
                        }
                    }

                    string personalizationTest = rendering.PersonalizationTest;
                    if (!string.IsNullOrEmpty(personalizationTest))
                    {
                        Item testDefinitionItem = this.InnerField.Database.GetItem(personalizationTest);
                        if (testDefinitionItem != null)
                        {
                            result.AddValidLink(testDefinitionItem, personalizationTest);
                        }
                        else
                        {
                            result.AddBrokenLink(personalizationTest);
                        }
                    }

                    if (renderingItem != null && !string.IsNullOrEmpty(rendering.Parameters))
                    {
                        var renderingParametersFieldCollection = this.GetParametersFields(renderingItem, rendering.Parameters);

                        foreach (var field in renderingParametersFieldCollection.Values)
                        {
                            field.ValidateLinks(result);
                        }
                    }

                    if (rendering.Rules != null)
                    {
                        var rulesField = new RulesField(this.InnerField, rendering.Rules.ToString());
                        rulesField.ValidateLinks(result);
                    }
                }
            }
        }
        private RenderingParametersFieldCollection GetParametersFields(Item layoutItem, string renderingParameters)
        {
            var urlParametersString = new UrlString(renderingParameters);
            RenderingParametersFieldCollection parametersFields;

            //layoutItem.Template.CreateItemFrom()

            RenderingParametersFieldCollection.TryParse(layoutItem, urlParametersString, out parametersFields);

            return parametersFields;
        }
    }
}