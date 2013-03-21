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

namespace ZoomifyDownloader
{
    class Dezoomify
    {

        public delegate void progressUpdate(int value);

        public static Stream DownloadStream( Stream outputStream, string url, string contentType )
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

        public static Bitmap Download(string url, progressUpdate updater) 
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
                Console.WriteLine("String escape: " + infoXMLFile);
                // Start download of the image information file.
                WebRequest request = WebRequest.Create(infoXMLFile);
                request.ContentType = "text/xml";
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

                //Found information file, continue to determine information about images.
                XmlDocument xhr = new XmlDocument();
                xhr.LoadXml(responseFromServer);
                XmlNode path = xhr.FirstChild.Attributes.GetNamedItem("path");
                XmlNodeList temp = xhr.GetElementsByTagName("IMAGE_PROPERTIES");
                if (temp.Count > 0)
                {
                    XmlNode width = temp[0].Attributes.GetNamedItem("WIDTH");
                    XmlNode height = temp[0].Attributes.GetNamedItem("HEIGHT");
                    XmlNode tilesize = temp[0].Attributes.GetNamedItem("TILESIZE");
                    XmlNode numtiles = temp[0].Attributes.GetNamedItem("NUMTILES");
                    return drawImage(url, temp[0], updater);
                }
            }
            else
            {
                Console.WriteLine("No URL specified.");                
            }
            return null;
        }


        static void remove(string el)
        {
        }
        // Overwrites a target image pixels with another image at location x,y, using the new image's resolution.
        public static Bitmap overlayImage(Bitmap target, Bitmap newImage, int x, int y)
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
        static void loadEnd()
        {
        }
        static void changeSize()
        {
        }
        static double findZoom(double size) 
        {
            //Fonction de BG
            return Math.Floor(Math.Log(size) / Math.Log(2) - 7);
        }
        static int findZoom(int size)
        {
            return System.Convert.ToInt32( Math.Floor( Math.Log(System.Convert.ToDouble(size)) / Math.Log(2) - 7 ) );
        }
        static Bitmap drawImage(string path, XmlNode information, progressUpdate loadingInformation)
        {
            int width = System.Convert.ToInt32(information.Attributes.GetNamedItem("WIDTH").Value);
            int height = System.Convert.ToInt32(information.Attributes.GetNamedItem("HEIGHT").Value);
            int tileSize = System.Convert.ToInt32( information.Attributes.GetNamedItem("TILESIZE").Value );
            int numTiles = System.Convert.ToInt32( information.Attributes.GetNamedItem("NUMTILES").Value );

           loadingInformation(0);

            //Create a canvas ( image array )
            Bitmap output = new Bitmap(width, height, System.Drawing.Imaging. PixelFormat.Format24bppRgb);
            int zoom = (width > height) ? findZoom(width) : findZoom(height);
            Console.WriteLine("Maximum Zoom Level: "+zoom);

            int nbrTilesX = (int) Math.Ceiling( (float)width / (float)tileSize );
            int nbrTilesY = (int) Math.Ceiling( (float)height / (float)tileSize );
            int loaded = 0;
            int totalTiles = nbrTilesX * nbrTilesY;
            int skippedTiles = numTiles - totalTiles;

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

                    string url = path + "/TileGroup" + tileGroup + "/" + zoom + "-" + x + "-" + y + ".jpg";                    
                    Console.WriteLine("GET: "+url);
                    //Add a tile image to the canvas.

                    //Method 0
                    //Stream tileDownload = null;
                    //tileDownload = DownloadStream(tileDownload, url, "image/jpeg");
                    //if (tileDownload.CanRead)
                    //{
                    //    Bitmap currentTile = new Bitmap(tileDownload);
                    //    output = overlayImage(output, currentTile, x * tileSize, y * tileSize);                        
                    //}
                    //tileDownload.Close();

                    //Method 1
                    WebRequest requestPic = WebRequest.Create(url);
                    WebResponse responsePic = requestPic.GetResponse();
                    Image webImage = Image.FromStream(responsePic.GetResponseStream()); // Error
                    if (webImage != null)
                    {
                        Bitmap currentTile = new Bitmap(webImage);
                        output = overlayImage(output, currentTile, x * tileSize, y * tileSize);     
                    }
                    responsePic.Close();
                    loadingInformation((int)((float)loaded / (float)numTiles) * 100);
                    loaded++;
                }
            }
            //Return image.
            return output;
        }
    }
}
