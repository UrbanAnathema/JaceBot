using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace JaceBot
{
    class CardService
    {
        public static string XmlLocation = @".\docs\cards.xml";
        private static string UrlLocation = @"http://thestaticvoid.github.io/storage/cards.xml";
        public static Card lastCard = null;
        public static List<Card> cardDB = new List<Card>();
        public static List<Set> setDB = new List<Set>();

        /* buildCardList()
         * This method deserializes the XML into a readable database of Card and Set objects
         * */
        public static void buildCardList()
        {
            if (!checkXML())
                update();
            cardDB.Clear();
            setDB.Clear();
            XmlSerializer serializer = new XmlSerializer(typeof(CardDatabase));
            CardDatabase input = serializer.Deserialize(new FileStream(XmlLocation, FileMode.Open)) as CardDatabase;
            cardDB = input.Cards[0].Card.Where(x => x.Name.Length > 0 && !(x.Type.Contains("Vanguard"))).ToList();
            setDB = input.Sets[0].Set.Where(x => x.Name.Length > 0).ToList();
            fixDB();
        }

        /* update()
         * This method is used to force the bot to update the stored cards.xml from a predefined location
         * */
        public static void update()
        {
            Console.WriteLine("[INFO] UPDATING CARD DATABASE");
            FileInfo cardXMLLocal = new FileInfo(XmlLocation);
            if (cardXMLLocal.Exists)
            {
                cardXMLLocal.Delete();
                downloadXML();
            }
            else
                downloadXML();
        }

        /* downloadXML()
         * This method downloads the new cards.xml from a predefined location
         * */
        private static void downloadXML()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(UrlLocation, XmlLocation);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        /* checkXML()
         * This method returns true if the cards.xml file exists within the bot
         * */
        private static bool checkXML() // returns true if the xml file already exists
        {
            FileInfo cardXMLLocal = new FileInfo(XmlLocation);
            if (cardXMLLocal.Exists)
                return true;
            else
                return false;
        }

        /* searchCard(string input)
         * This method looks through the entire cardDB List<Card> to find any card that has a name based on the searched text. There are several ways the bot will do this:
         * 1.) Any card that contains the searched text is returned
         * 2.) Any card that contains the searched text split around the ',' character is returned
         * 3.) If the searched text is an exact match to a card name, only return that one card. This is used for cards such as "Stasis" and "Stasis Snare"
         * */
        public static List<Card> searchCard(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            List<Card> results = cardDB.Where(card => input.Split(',').All(text => card.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0)).ToList();
            foreach (Card result in results)
                if (string.Equals(result.Name, input, StringComparison.InvariantCultureIgnoreCase))
                    return new List<Card> { result };
            return results;
        }

        /* fixDB()
         * This method exists because the card colors were being deserialized improperly, so this mainly goes through and builds their colors based on what their manacost is
         * This method also fixes the tab character and any extra spaces existing within the description text of the card
         * This method also fixes the PT of certain cards like Tarmogoyf being displayed improperly in Discord
         * */
        private static void fixDB()
        {
            foreach (Card card in cardDB)
            {
                string newColor = "";
                if (card.ManaCost.Contains("W"))
                    newColor += "W";
                if (card.ManaCost.Contains("U"))
                    newColor += "U";
                if (card.ManaCost.Contains("R"))
                    newColor += "R";
                if (card.ManaCost.Contains("G"))
                    newColor += "G";
                if (card.ManaCost.Contains("B"))
                    newColor += "B";
                if (!(card.ManaCost.Contains("W") || card.ManaCost.Contains("U") || card.ManaCost.Contains("R") || card.ManaCost.Contains("G") || card.ManaCost.Contains("B")))
                    newColor = "";

                card.Color = newColor;
                // since this method is already called while the card database is built, I'm going to also use it to adjust the issues with descriptions.
                string descReg = @"^(?:[\t ]*(?:\r?\n|\r)) + |( {2,})";
                card.Text = Regex.Replace(card.Text, descReg, string.Empty);
                if (!(string.IsNullOrEmpty(card.PT)))
                    card.PT = card.PT.Replace("*", @"\*"); // this is called to fix cards like Tarmogoyf
            }
        }
        
        /* GetCardColor(Card card)
         * Based on the colors of the Card, this will return a Color to have the embed be for the returned card
         * */
        public static Color GetCardColor(Card card)
        {
            if (card.Color.Length > 1)
                return new Color(255, 230, 41); // returns gold
            if (card.Color.Length < 1)
                return new Color(150, 150, 150); // returns gray
            Color col;
            switch (card.Color)
            {
                case "W":
                    col = new Color(240, 240, 240);
                    break;
                case "U":
                    col = new Color(51, 204, 255);
                    break;
                case "R":
                    col = new Color(255, 50, 50);
                    break;
                case "G":
                    col = new Color(4, 148, 47);
                    break;
                case "B":
                    col = new Color(48, 48, 48);
                    break;
                default:
                    col = new Color(200, 23, 207);
                    break;
            }
            return col;
        }
        
        /* GetCardImage(Card card)
         * This is used to find the link to the card in order to use it as a thumbnail in the embed of the card.
         * */
        public static string GetCardImage(Card card)
        {
            string muid = "";
            foreach (CardSet line in card.CardSet)
                if (!(line.MuID.Equals("0")))
                    muid = line.MuID;
            return $"http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={muid}&type=card";
        }
        
        /* GetGathererLink(Card card)
         * This is used to return the link to the official Gatherer Page for the card to be used as the hyperlink in the name of the card.
         * */
        public static string GetGathererLink(Card card)
        {
            string muid = "";
            foreach (CardSet line in card.CardSet)
                if (!(line.MuID.Equals("0")))
                    muid = line.MuID;
            return $"http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid={muid}";
        }
    }
}
