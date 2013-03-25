/*
 * Based on the Dezoomify javascript by lovasoa. 
 * https://gist.github.com/lovasoa/770310 
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;

namespace ZoomifyDownloader
{

    public struct ImageProperties
    {
        public string name;
        public string path;
        public int width;
        public int height;
        public int numTiles;
        public int numImages;
        public float version;
        public int tileSize;

        public ImageProperties(string name, string path, int width, int height, int numTiles, int numImages, float version, int tileSize)
        {
            this.name = name;
            this.path = path;
            this.width = width;
            this.height = height;
            this.numTiles = numTiles;
            this.numImages = numImages;
            this.version = version;
            this.tileSize = tileSize;
        }
        // Override the ToString method:
        public override string ToString() 
        {
            return String.Format("({0},{1},{2},{3},{4},{5},{6},{7})", name, path, width, height, numTiles, numImages, version, tileSize);
        }
    }

    public enum SaveFileConflictMode
    {
        Ask = 0,
        DoNothing,
        Replace,
        Increment,
        ReplaceAlways,
        IncrementAlways,
    }

    class Dezoomify
    {
        // Delegate for informing progress changes on the download.
        public delegate void progressUpdate(int value);
     
        // Starts the process for downloading a zoomify image.
        public static Bitmap Download(string url) 
        {
            if (url != null && url.Length > 0)
            {
                string infoXMLFile = url;
                //Zoomify Images are stored in pieces with a control file.
                //Determine if the path is already linking to the proper file or not.
                if (!infoXMLFile.Contains("ImageProperties.xml"))
                {
                    infoXMLFile += "/ImageProperties.xml";
                }
                Console.WriteLine("Hello, Download operation starting on: " + infoXMLFile);
                url = Uri.EscapeUriString(url);
                infoXMLFile = Uri.EscapeUriString(infoXMLFile);

                string responseFromServer = DownloadFile(infoXMLFile);

                if (responseFromServer.Length > 0)
                {
                    string name = url.Substring( url.LastIndexOf("/") + 1 );
                    ImageProperties fileData = GetImageProperties(responseFromServer, name, url);
                    return assembleImage(fileData);
                }
                
            }
            else
            {
                Console.WriteLine("No URL specified.");                
            }
            return null;
        }

        // Save an image to file with file replacement protection settings.
        public static string Save(Bitmap image, ImageFormat format, string path, string filename, SaveFileConflictMode method)
        {
            // Is the textbox3 an absolute or relative path?
            string fileExtension = "." + format.ToString().ToLowerInvariant();            
            if (Dezoomify.StringIsLast(filename, fileExtension))
            {
                filename = filename.Remove(filename.Length - fileExtension.Length);
            }
            string saveFilePath = path + "\\" + filename + fileExtension;
            switch (method)
            {
                case SaveFileConflictMode.Replace:
                case SaveFileConflictMode.ReplaceAlways:
                    break;
                case SaveFileConflictMode.Increment:
                case SaveFileConflictMode.IncrementAlways:
                default:
                    int fileCount = 0;
                    while (File.Exists(saveFilePath))
                    {
                        fileCount++;
                        saveFilePath = path + "\\" + filename + " (" + fileCount.ToString() + ")" + fileExtension;
                    }
                    break;
                case SaveFileConflictMode.DoNothing:
                    Console.WriteLine("Canceled download");
                    return "";
            }           
            Console.WriteLine("Finished download: " + saveFilePath);
            image.Save(saveFilePath, format);
            return saveFilePath;
        }

        // Check if a given substring is at the very end of the string.
        public static bool StringIsLast(string target, string substring)
        {
            if (target.Contains(substring) && target.LastIndexOf(substring) == (target.Length - substring.Length))
            {
                return true;
            }
            return false;
        }
    
        // Downloads a file and returns it's contents.
        public static string DownloadFile(string url)
        {
            string responseFromServer = "";
            if (url != null && url.Length > 0)
            {
                try
                {                    
                    url = Uri.EscapeUriString(url);
                    // Start download of the image information file.
                    WebRequest request = WebRequest.Create(url);
                    request.ContentType = "text";
                    request.Method = "GET";
                    // If required by the server, set the credentials.
                    request.Credentials = CredentialCache.DefaultCredentials;
                    // Get the response.
                    using (WebResponse response = request.GetResponse())
                    {
                        // Display the status.
                        Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                        // Get the stream containing content returned by the server.
                        Stream dataStream = response.GetResponseStream();
                        // Open the stream using a StreamReader for easy access.
                        using (StreamReader reader = new StreamReader(dataStream))
                        {
                            // Read the content.
                            responseFromServer = reader.ReadToEnd();
                            // Display the content.
                            Console.WriteLine(responseFromServer);
                            // Clean up the streams and the response.
                            reader.Close();
                        }
                        response.Close();
                    }
                }
                catch
                {
                    return "";
                }
            }
            return responseFromServer;
        }

        // Downloads a Stream - has to be closed manually.
        public static Stream DownloadStream(Stream outputStream, string url, string contentType)
        {
            WebRequest request = WebRequest.Create(url);
            request.ContentType = contentType;
            request.Method = "GET";
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            using (WebResponse response = request.GetResponse())
            {
                // Display the status.
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.
                outputStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                //StreamReader reader = new StreamReader(outputStream);
                // Read the content.
                //string responseFromServer = reader.ReadToEnd();
                // Display the content.
                //Console.WriteLine(responseFromServer);
                // Clean up the streams and the response.
                //reader.Close();
                response.Close();
            }
            return outputStream;
        }

        // Sees if a file called ImageProperties exists at a given url.
        public static bool TryGetImageProperties(string url)
        {
            bool fileExists = false;
            //Zoomify Images are stored in pieces with a control file.
            //Determine if the path is already linking to the proper file or not.
            if (!url.Contains("ImageProperties.xml"))
            {
                url += "/ImageProperties.xml";
            }
            url = Uri.EscapeUriString(url);
            if (FileStatus(url) == HttpStatusCode.OK)
            {
                fileExists = true;
            }
            return fileExists;
        }

        // Retrieves image properties given an XML file.
        public static ImageProperties GetImageProperties(string xmldata, string name, string path)
        {
            if (xmldata != null && xmldata.Length > 0)
            {
                //Create the output structure (empty)
                ImageProperties output = new ImageProperties();
                //Convert file data into XML format for easy property access.
                XmlDocument xhr = new XmlDocument();
                xhr.LoadXml(xmldata);
                //Only count from the first found Image Properties Element.
                XmlNodeList temp = xhr.GetElementsByTagName("IMAGE_PROPERTIES");                
                if (temp.Count > 0)
                {
                    //Fill the output structure.
                    output.name = name;
                    output.path = path;
                    output.width = System.Convert.ToInt32(temp[0].Attributes.GetNamedItem("WIDTH").Value);
                    output.height = System.Convert.ToInt32(temp[0].Attributes.GetNamedItem("HEIGHT").Value);
                    output.numImages = System.Convert.ToInt32(temp[0].Attributes.GetNamedItem("NUMIMAGES").Value);
                    output.numTiles = System.Convert.ToInt32(temp[0].Attributes.GetNamedItem("NUMTILES").Value);
                    output.version = System.Convert.ToSingle(temp[0].Attributes.GetNamedItem("VERSION").Value);
                    output.tileSize = System.Convert.ToInt32(temp[0].Attributes.GetNamedItem("TILESIZE").Value);
                }
                return output;
            }
            return new ImageProperties();
        }

        // Retrieves the urls of all images used in the maximum level zoomify viewer.
        public static string[] GetImageList(ImageProperties imageData)
        {
            List<string> output = new List<string>();
            int zoom = (imageData.width > imageData.height) ? findZoom(imageData.width) : findZoom(imageData.height);
            int nbrTilesX = (int)Math.Ceiling((float)imageData.width / (float)imageData.tileSize);
            int nbrTilesY = (int)Math.Ceiling((float)imageData.height / (float)imageData.tileSize);
            int totalTiles = nbrTilesX * nbrTilesY;
            int skippedTiles = imageData.numTiles - totalTiles;

            int tileGroup = (int) Math.Floor( (float)skippedTiles / 256 ); // Original script divides by 256. Perhaps it refers to the tile size?
            int tileGroupCounter = (int)((float)skippedTiles % 256); // Original script divides by 256. Perhaps it refers to the tile size?

            int x, y;
            for (y = 0; y < nbrTilesY; y++)
            {
                for (x = 0; x < nbrTilesX; x++)
                {

                    if (tileGroupCounter >= 256)
                    {
                        tileGroup++;
                        tileGroupCounter = 0;
                    }

                    tileGroupCounter++;

                    output.Add(imageData.path + "/TileGroup" + tileGroup + "/" + zoom + "-" + x + "-" + y + ".jpg");
                }
            }
            return output.ToArray();
        }

        // Tests a URL to see if it is absolute or not.
        public static bool isAbsoluteURL(string url)
        {
            try
            {
                Uri test = new Uri(url);
                return test.IsAbsoluteUri;
            }
            catch
            {
                return false;
            }            
        }


        // Attempt at getting XmlDocument to work parsing html. Failed so this is now useless.
        public static string RemoveDocType(string xmlData)
        {
            string search = ("<!DOCTYPE").ToLowerInvariant();
            if(xmlData.ToLowerInvariant().Contains(search))
            {
                int start, end;
                start = xmlData.ToLowerInvariant().IndexOf(search, 0);
                end = xmlData.ToLowerInvariant().IndexOf(">", start + search.Length);
                xmlData = xmlData.Remove(start, end - start + 1);
            }
            return xmlData;
        }

        // XmlReader with my test html is broken. Useless.
        public static string[] GetElementAttributeOfName(string xmlData, string elementName, string attributeName)
        {
            List<string> output = new List<string>();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = null;
            settings.DtdProcessing = DtdProcessing.Ignore;
            using (XmlReader htmldoc = XmlReader.Create(new StringReader(xmlData), settings))
            {
                while (htmldoc.Read())
                {
                    /*
                    switch (htmldoc.NodeType)
                    {
                        case XmlNodeType.Element:
                            Console.WriteLine("Start Element {0}", htmldoc.Name);
                            break;
                        case XmlNodeType.Attribute:
                            //Console.WriteLine("Start Value {0}", htmldoc.GetAttribute("href"));
                            break;
                        case XmlNodeType.Text:
                            Console.WriteLine("Text Node: {0}", htmldoc.Value);
                            break;
                        case XmlNodeType.EndElement:
                            Console.WriteLine("End Element {0}", htmldoc.Name);
                            break;
                        default:

                            break;
                    }
                     * */
                    if (htmldoc.NodeType == XmlNodeType.Element && htmldoc.Name == elementName && htmldoc.GetAttribute(attributeName).Length > 0)
                    {
                        output.Add(htmldoc.GetAttribute(attributeName));
                    }
                }
            }   
            return output.ToArray();
        }

        // Hack code to grab all links on webpage. Don't want to include dependencies on external HTML parsing library.
        private static string[] GetLinks(string htmlData)
        {
            List<string> output = new List<string>();
            int iterator = 0;
            while (iterator < htmlData.Length)
            {
                if (htmlData.Substring(iterator).Contains("<a"))
                {
                    int start = htmlData.IndexOf("<a", iterator);
                    int end = htmlData.IndexOf(">", start);
                    string element = htmlData.Substring(start, end - start + 1);
                    if( element.Contains("href") )
                    {
                        int valueStart = element.IndexOf("href");
                        valueStart = element.IndexOf("\"", valueStart);
                        int valueEnd = element.IndexOf("\"", valueStart + 1);
                        string link = element.Substring(valueStart, valueEnd - valueStart + 1);
                        if (!link.Contains("javascript") && !link.Contains("#"))
                        {
                            if (link.Contains("."))
                            {
                                if (link.LastIndexOf(".") < link.LastIndexOf("/"))
                                {
                                    output.Add(link);
                                }
                            }
                            else
                            {
                                output.Add(link);
                            }
                        }                        
                    }
                    iterator = end;
                }
                else
                {
                    iterator = htmlData.Length;
                }
            }
            return output.ToArray();
        }

        // Checks if input is an absolute url, fixes that if not.
        private static string ToAbsoluteURL(string input, string domain)
        {
            // Getting rid of junk.
            input = input.Replace("\"", "");
            input = input.Trim();            
            // Is absolute already ?
            if (input.Contains("http://") )
            {
                return input;
            }
            while (input.IndexOf("//") == 0)
            {
                Console.WriteLine("This is a relative path that moves back a directory.");
                domain = "http://" + new Uri(domain).Host;
                input = input.Remove(0, 1);
                input = domain + input;
            }
            if (input.IndexOf("/") == 0)
            {
                Console.WriteLine("This is a relative path that uses the current dirctory.");
                if(domain.LastIndexOf("/") == domain.Length - 1)                
                {
                    domain = domain.Remove(domain.Length - 1);
                }
                //domain.Substring(0, domain.LastIndexOf("/"));
                input = domain + input;
            }
            return input;
        }

        // Checks if the domain of input is the same as the one specified.
        public static bool MatchDomain(string input, string domain)
        {
            return (new Uri(input).Host == new Uri(domain).Host);
        }

        // Extracts all direct links to zoomify URLs.
        public static string[] ExtractURLs(string baseUrl, string htmlData)
        {
            List<string> zoomifyURLs = new List<string>();

            List<string> links = new List<string>();
            links.AddRange( GetLinks(htmlData) );
            // Remove any duplicates
            for (int i = links.Count - 1; i > 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    if (links[i] == links[j])
                    {
                        links.RemoveAt(i);
                        break;
                    }
                }
            }
            // Sanitize Links
            for (int i = links.Count - 1; i > 0; i--)
            {
                links[i] = ToAbsoluteURL(links[i], baseUrl);
                if (!MatchDomain(links[i], baseUrl))
                {
                    links.RemoveAt(i);
                }
                else if (TryGetImageProperties(links[i]))
                {
                    zoomifyURLs.Add(links[i]);
                }
                else
                {
                    Console.WriteLine(links[i] + " was not a zoomify url.");
                }
            }
            zoomifyURLs.Reverse();
            return zoomifyURLs.ToArray();
        }

        // Overwrites a target image pixels with another image at location x,y, using the new image's resolution.
        public static Bitmap OverlayImage(Bitmap target, Bitmap newImage, int x, int y)
        {
            for (int i = 0; i < newImage.Width; i++)
            {
                for (int j = 0; j < newImage.Height; j++)
                {
                    target.SetPixel( x+i, y+j, newImage.GetPixel(i,j) );
                }
            }
            return target;
        }

        // Finds the maximum zoom level given the largest dimension of the full resolution image
        static double findZoom(double size) 
        {
            //Fonction de BG
            return Math.Floor(Math.Log(size) / Math.Log(2) - 7);
        }
        static int findZoom(int size)
        {
            // The formula for calculating zoom is not 100% accurate to the zoomify algorithmn.
            // Given the input an image 2734 x 4096, it returns a zoom level of 5 when it should return a 4.
            // This is the only case where I found the zoom level was incorrectly calculated so far.
            // To handle this, on exception it will try a single zoom level smaller.
            return System.Convert.ToInt32( Math.Floor( Math.Log(System.Convert.ToDouble(size)) / Math.Log(2) - 7 ) );
        }

        // Test to find if the file path is good or not.
        static HttpStatusCode FileStatus(string url)
        {
            HttpStatusCode result = HttpStatusCode.BadRequest;
            HttpWebRequest requestPic =  (HttpWebRequest) WebRequest.Create(url);
            try
            {
                using (HttpWebResponse responsePic = (HttpWebResponse)requestPic.GetResponse())
                {
                    result = responsePic.StatusCode;
                    responsePic.Close();
                }
            }
            catch (System.Net.WebException ex)
            {
                Console.WriteLine(ex.ToString());
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    result = ((HttpWebResponse)ex.Response).StatusCode;
                } 
                else
                {
                    result = HttpStatusCode.BadRequest;
                }
            }
            return result;
        }

        static Bitmap GetTile(string url)
        {
            Bitmap result = null;
            WebRequest requestPic = WebRequest.Create(url);
            using (WebResponse responsePic = requestPic.GetResponse())
            {
                Console.WriteLine("GET: " + url);
                Image webImage = Image.FromStream(responsePic.GetResponseStream());
                if (webImage != null)
                {
                    result = new Bitmap(webImage);
                }
                responsePic.Close();
            }
            return result;
        }

        // Builds an image from multiple sources.
        static Bitmap assembleImage(ImageProperties imageData)
        {
            //Create a canvas ( image array )
            Bitmap output = new Bitmap(imageData.width, imageData.height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            int zoom = (imageData.width > imageData.height) ? findZoom(imageData.width) : findZoom(imageData.height);

            int nbrTilesX = (int)Math.Ceiling((float)imageData.width / (float)imageData.tileSize);
            int nbrTilesY = (int)Math.Ceiling((float)imageData.height / (float)imageData.tileSize);
            int loaded = 0;
            int totalTiles = nbrTilesX * nbrTilesY;
            int skippedTiles = imageData.numTiles - totalTiles;

            int tileGroup = (int) Math.Floor( (float)skippedTiles / 256 ); // Original script divides by 256. Perhaps it refers to the tile size?
            int tileGroupCounter = (int)((float)skippedTiles % 256); // Original script divides by 256. Perhaps it refers to the tile size?

            int x, y;
            for (y = 0; y < nbrTilesY; y++)
            {
                for (x = 0; x < nbrTilesX; x++)
                {

                    if (tileGroupCounter >= 256)
                    {
                        tileGroup++;
                        tileGroupCounter = 0;
                    }

                    tileGroupCounter++;
                    string url = imageData.path + "/TileGroup" + tileGroup + "/" + zoom + "-" + x + "-" + y + ".jpg";
                    HttpStatusCode result = FileStatus(url);
                    try
                    {   
                        //Try all zoom levels under the current.
                        while( result != HttpStatusCode.OK && zoom > 0)
                        {
                            if (result == HttpStatusCode.NotFound)
                            {
                                zoom--;
                                url = imageData.path + "/TileGroup" + tileGroup + "/" + zoom + "-" + x + "-" + y + ".jpg";
                                result = FileStatus(url);
                            }
                            else
                            {
                                Console.WriteLine("Unexpected server error : " + FileStatus(url).ToString()); 
                            }
                        }
                        //Add a tile image to the canvas.
                        if (result == HttpStatusCode.OK)
                        {
                            output = OverlayImage(output, GetTile(url), x * imageData.tileSize, y * imageData.tileSize);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("I don't know what happened.");
                    }
                    
                }
            }
            //Return image.
            return output;
        }
    }
}
