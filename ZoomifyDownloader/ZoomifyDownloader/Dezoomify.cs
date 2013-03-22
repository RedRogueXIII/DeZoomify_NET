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
            string saveFilePath = path + "\\" + filename + "." + format.ToString().ToLower();
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
                        saveFilePath = path + "\\" + filename + " (" + fileCount.ToString() + ")" + "." + format.ToString().ToLower();
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
    
        // Downloads a file and returns it's contents.
        public static string DownloadFile(string url)
        {
            if (url != null && url.Length > 0)
            {
                url = Uri.EscapeUriString(url);
                // Start download of the image information file.
                WebRequest request = WebRequest.Create(url);
                request.ContentType = "text";
                request.Method = "GET";
                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
                // Clean up the streams and the response.
                reader.Close();
                response.Close();
                return responseFromServer;
            }
            return "";
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
            WebResponse response = request.GetResponse();
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
            return outputStream;
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
            return System.Convert.ToInt32( Math.Floor( Math.Log(System.Convert.ToDouble(size)) / Math.Log(2) - 7 ) );
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
                    Console.WriteLine("GET: "+url);
                    //Add a tile image to the canvas.

                    //Method 1
                    WebRequest requestPic = WebRequest.Create(url);
                    WebResponse responsePic = requestPic.GetResponse();
                    Image webImage = Image.FromStream(responsePic.GetResponseStream()); // Error
                    if (webImage != null)
                    {
                        Bitmap currentTile = new Bitmap(webImage);
                        output = OverlayImage(output, currentTile, x * imageData.tileSize, y * imageData.tileSize);     
                    }
                    responsePic.Close();
                    //loadingInformation((int)((float)loaded / (float)numTiles) * 100);
                    loaded++;
                }
            }
            //Return image.
            return output;
        }
    }
}
