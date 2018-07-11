using Sitecore.Pipelines.RenderField;
using Sitecore.Xml.Xsl;

namespace Sitecore.Foundation.Extension.MediaType.Pipeline
{
    public class GetImageFieldValue
    {
        /// <summary>
        /// Gets the field value.
        /// 
        /// </summary>
        /// <param name="args">The arguments.</param><contract><requires name="args" condition="none"/></contract>
        public void Process(RenderFieldArgs args)
        {
            if (args.FieldTypeKey != "image")
            {
                return;
            }

            ImageRendererEx renderer = CreateRenderer();
            renderer.Item = args.Item;
            renderer.FieldName = args.FieldName;
            renderer.FieldValue = args.FieldValue;
            renderer.Parameters = args.Parameters;
            args.WebEditParameters.AddRange(args.Parameters);
            renderer.Parameters.Add("la", args.Item.Language.Name);
            RenderFieldResult renderFieldResult = renderer.Render();
            args.Result.FirstPart = renderFieldResult.FirstPart;
            args.Result.LastPart = renderFieldResult.LastPart;
            args.DisableWebEditContentEditing = true;
            args.DisableWebEditFieldWrapping = true;
            args.WebEditClick = "return Sitecore.WebEdit.editControl($JavascriptParameters, 'webedit:chooseimage')";
        }

        /// <summary>
        /// Creates the renderer.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// The renderer.
        /// </returns>
        protected virtual ImageRendererEx CreateRenderer()
        {
            return new ImageRendererEx();
        }
    }
}