using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Resources.Media;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Xml.Xsl;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Version = Sitecore.Data.Version;

namespace Sitecore.Foundation.Extension.MediaType.Pipeline
{
    public class ImageRendererEx : FieldRendererBase
    {
        private string alt;
        private bool allowStretch;
        private bool asSet;
        private string backgroundColor;
        private string border;
        private string className;
        private string database;
        private bool disableMediaCache;
        private bool disableMediaCacheSet;
        private string fieldName;
        private string fieldValue;
        private int height;
        private bool heightSet;
        private string hspace;
        private bool ignoreAspectRatio;
        private ImageField imageField;
        private Item item;
        private string language;
        private int maxHeight;
        private bool maxHeightSet;
        private int maxWidth;
        private bool maxWidthSet;
        private SafeDictionary<string> parameters;
        private float scale;
        private bool scaleSet;
        private string source;
        private bool thumbnail;
        private bool thumbnailSet;
        private string version;
        private string vspace;
        private int width;
        private bool widthSet;
        private bool xhtml;

        public string FieldName
        {
            get { return fieldName; }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                fieldName = value;
            }
        }

        public string FieldValue
        {
            get { return fieldValue; }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                fieldValue = value;
            }
        }

        public Item Item
        {
            get { return item; }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                item = value;
            }
        }

        public SafeDictionary<string> Parameters
        {
            get { return parameters; }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                parameters = value;
            }
        }

        public virtual RenderFieldResult Render()
        {
            var obj = Item;
            if (obj == null)
            {
                return RenderFieldResult.Empty;
            }

            var keyValuePairs = Parameters;

            if (keyValuePairs == null)
            {
                return RenderFieldResult.Empty;
            }

            ParseNode(keyValuePairs);

            var innerField = obj.Fields[FieldName];

            if (innerField != null)
            {
                imageField = new ImageField(innerField, FieldValue);

                ParseField(imageField);
                AdjustImageSize(imageField, scale, maxWidth, maxHeight, ref width,
                    ref height);
                
                if (imageField.MediaItem != null)
                {
                    MediaItem imageMediaItem = new MediaItem(imageField.MediaItem);

                    if (imageMediaItem.MimeType == "image/svg+xml")
                    {
                        return new RenderFieldResult(RenderSvgImage(imageMediaItem));
                    }
                }
            }

            var site = Context.Site;

            if ((string.IsNullOrEmpty(source) || IsBroken(imageField)) && site != null &&
                site.DisplayMode == DisplayMode.Edit)
            {
                source = GetDefaultImage();
                className += " scEmptyImage";
                className = className.TrimStart(' ');
            }

            if (string.IsNullOrEmpty(source))
            {
                return RenderFieldResult.Empty;
            }

            var imageSource = GetSource();
            var stringBuilder = new StringBuilder("<img");
            AddAttribute(stringBuilder, "src", imageSource);
            AddAttribute(stringBuilder, "border", border);
            AddAttribute(stringBuilder, "hspace", hspace);
            AddAttribute(stringBuilder, "vspace", vspace);
            AddAttribute(stringBuilder, "class", className);
            AddAttribute(stringBuilder, "alt", HttpUtility.HtmlAttributeEncode(alt), xhtml);
            if (width > 0)
            {
                AddAttribute(stringBuilder, "width", width.ToString());
            }

            if (height > 0)
            {
                AddAttribute(stringBuilder, "height", height.ToString());
            }

            CopyAttributes(stringBuilder, keyValuePairs);
            stringBuilder.Append(" />");
            return new RenderFieldResult(stringBuilder.ToString());
        }

        protected virtual void AdjustImageSize(ImageField imageField, float imageScale, int imageMaxWidth, int imageMaxHeight, ref int w, ref int h)
        {
            Assert.ArgumentNotNull(imageField, "imageField");
            var int1 = MainUtil.GetInt(imageField.Width, 0);
            var int2 = MainUtil.GetInt(imageField.Height, 0);
            if (int1 == 0 || int2 == 0)
                return;
            var size = new Size(w, h);
            var imageSize = new Size(int1, int2);
            var maxSize = new Size(imageMaxWidth, imageMaxHeight);
            var finalImageSize = GetFinalImageSize(GetInitialImageSize(imageSize, imageScale, size), size,
                maxSize);
            w = finalImageSize.Width;
            h = finalImageSize.Height;
        }

        protected virtual void CopyAttributes(StringBuilder result, SafeDictionary<string> attributes)
        {
            Assert.ArgumentNotNull(result, "result");
            Assert.ArgumentNotNull(attributes, "attributes");
            foreach (var keyValuePair in attributes)
            {
                if (keyValuePair.Key != "field" && keyValuePair.Key != "select" && keyValuePair.Key != "outputMethod")
                    AddAttribute(result, keyValuePair.Key, keyValuePair.Value);
            }
        }

        protected virtual string Extract(SafeDictionary<string> values, params string[] keys)
        {
            Assert.ArgumentNotNull(values, "values");
            Assert.ArgumentNotNull(keys, "keys");
            foreach (var key in keys)
            {
                var str = values[key];
                if (str != null)
                {
                    values.Remove(key);
                    return str;
                }
            }
            return null;
        }

        protected virtual string GetDefaultImage()
        {
            return
                Themes.MapTheme(
                    Sitecore.Client.GetItemNotNull("/sitecore/content/Applications/WebEdit/WebEdit Texts",
                        Sitecore.Client.CoreDatabase)["Default Image"]);
        }

        protected virtual Size GetFinalImageSize(Size imageSize, Size size, Size maxSize)
        {
            if (maxSize.IsEmpty)
                return imageSize;
            if (maxSize.Width > 0 && imageSize.Width > maxSize.Width)
            {
                if (size.Height == 0)
                    imageSize.Height =
                        (int)Math.Round(maxSize.Width / (double)imageSize.Width * imageSize.Height);
                imageSize.Width = maxSize.Width;
            }
            if (maxSize.Height > 0 && imageSize.Height > maxSize.Height)
            {
                if (size.Width == 0)
                    imageSize.Width =
                        (int)Math.Round(maxSize.Height / (double)imageSize.Height * imageSize.Width);
                imageSize.Height = maxSize.Height;
            }
            return imageSize;
        }

        protected virtual Size GetInitialImageSize(Size imageSize, float imageScale, Size size)
        {
            if (imageScale > 0.0)
                return new Size(Scale(imageSize.Width, imageScale), Scale(imageSize.Height, imageScale));
            if (size.IsEmpty || size == imageSize)
                return imageSize;
            if (size.Width == 0)
            {
                var scaleNumber = size.Height / (float)imageSize.Height;
                return new Size(Scale(imageSize.Width, scaleNumber), size.Height);
            }
            if (size.Height != 0)
                return new Size(size.Width, size.Height);
            var scaleNumber1 = size.Width / (float)imageSize.Width;
            return new Size(size.Width, Scale(imageSize.Height, scaleNumber1));
        }

        protected virtual string GetSource()
        {
            var options = new MediaUrlOptions();
            Language result1;

            if (!string.IsNullOrEmpty(language) && Language.TryParse(language, out result1))
            {
                options.Language = result1;
            }

            if (!string.IsNullOrEmpty(database))
            {
                options.Database = Factory.GetDatabase(database);
            }

            Version result2;
            if (Version.TryParse(version, out result2))
            {
                options.Version = result2;
            }

            options.Width = width;
            options.Height = height;

            if (maxHeightSet)
            {
                options.MaxHeight = maxHeight;
            }

            if (maxWidthSet)
            {
                options.MaxWidth = maxWidth;
            }

            if (thumbnailSet)
            {
                options.Thumbnail = thumbnail;
            }

            if (scaleSet)
            {
                options.Scale = scale;
            }
            if (asSet)
            {
                options.AllowStretch = allowStretch;
            }
            if (!string.IsNullOrEmpty(backgroundColor))

                options.BackgroundColor = MainUtil.StringToColor(backgroundColor);
            options.IgnoreAspectRatio = ignoreAspectRatio;
            if (disableMediaCacheSet)
                options.DisableMediaCache = disableMediaCache;
            var urlString = imageField.MediaItem == null
                ? new UrlString(source)
                : new UrlString(MediaManager.GetMediaUrl(imageField.MediaItem, options));
            var parameters = new UrlString(options.ToString()).Parameters;
            foreach (var key in parameters.AllKeys)
                urlString.Append(key, parameters[key]);
            return urlString.GetUrl(xhtml && Settings.Rendering.ImagesAsXhtml);
        }

        protected virtual bool IsBroken(ImageField field)
        {
            if (field == null)
                return false;
            return field.MediaItem == null;
        }

        protected virtual void ParseField(ImageField imageFieldParse)
        {
            Assert.ArgumentNotNull(imageFieldParse, "imageFieldParse");
            if (!string.IsNullOrEmpty(database))
                imageFieldParse.MediaDatabase = Factory.GetDatabase(database);
            if (!string.IsNullOrEmpty(language))
                imageFieldParse.MediaLanguage = Language.Parse(language);
            if (!string.IsNullOrEmpty(version))
                imageFieldParse.MediaVersion = Version.Parse(version);
            if (imageFieldParse.MediaItem != null)
                source = StringUtil.GetString(source, imageFieldParse.MediaItem.Paths.FullPath);
            alt = StringUtil.GetString(alt, imageFieldParse.Alt);
            border = StringUtil.GetString(border, imageFieldParse.Border);
            hspace = StringUtil.GetString(hspace, imageFieldParse.HSpace);
            vspace = StringUtil.GetString(vspace, imageFieldParse.VSpace);
            className = StringUtil.GetString(className, imageFieldParse.Class);
        }

        protected virtual void ParseNode(SafeDictionary<string> attributes)
        {
            Assert.ArgumentNotNull(attributes, "attributes");
            var str = Extract(attributes, "outputMethod");
            xhtml = str == "xhtml" || Settings.Rendering.ImagesAsXhtml && str != "html";
            source = Extract(attributes, "src");
            alt = Extract(attributes, "alt");
            border = Extract(attributes, "border");
            hspace = Extract(attributes, "hspace");
            vspace = Extract(attributes, "vspace");
            className = Extract(attributes, "class");
            if (string.IsNullOrEmpty(border) && !xhtml)
                border = "0";
            allowStretch = MainUtil.GetBool(Extract(attributes, ref asSet, "allowStretch", "as"), false);
            ignoreAspectRatio = MainUtil.GetBool(Extract(attributes, "ignoreAspectRatio", "iar"), false);
            width = MainUtil.GetInt(Extract(attributes, ref widthSet, "width", "w"), 0);
            height = MainUtil.GetInt(Extract(attributes, ref heightSet, "height", "h"), 0);
            scale = MainUtil.GetFloat(Extract(attributes, ref scaleSet, "scale", "sc"), 0.0f);
            maxWidth = MainUtil.GetInt(Extract(attributes, ref maxWidthSet, "maxWidth", "mw"), 0);
            maxHeight = MainUtil.GetInt(Extract(attributes, ref maxHeightSet, "maxHeight", "mh"), 0);
            thumbnail = MainUtil.GetBool(Extract(attributes, ref thumbnailSet, "thumbnail", "thn"), false);
            backgroundColor = Extract(attributes, "backgroundColor", "bc") ?? string.Empty;
            database = Extract(attributes, "database", "db");
            language = Extract(attributes, "language", "la");
            version = Extract(attributes, "version", "vs");
            disableMediaCache = MainUtil.GetBool(Extract(attributes, ref disableMediaCacheSet, "disableMediaCache"), false);
        }

        protected virtual int Scale(int value, float scaleNumber)
        {
            return (int)Math.Round(value * (double)scaleNumber);
        }

        private string Extract(SafeDictionary<string> values, ref bool valueSet, params string[] keys)
        {
            Assert.ArgumentNotNull(values, "values");
            Assert.ArgumentNotNull(keys, "keys");
            var str = Extract(values, keys);
            valueSet = str != null;
            return str;
        }

        private string RenderSvgImage(MediaItem mediaItem)
        {
            Assert.ArgumentNotNull(mediaItem, "mediaItem");

            string result;

            using (StreamReader reader = new StreamReader(mediaItem.GetMediaStream(), Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }

            XDocument svg = XDocument.Parse(result);

            if (svg.Document?.Root != null)
            {
                if (width > 0)
                {
                    svg.Document.Root.SetAttributeValue("width", width);
                }

                if (height > 0)
                {
                    svg.Document.Root.SetAttributeValue("height", height);
                }

                result = svg.ToString();
            }

            return result;
        }
    }
}