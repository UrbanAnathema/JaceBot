using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace JaceBot
{
    // Serializing XML makes me suicidal
    [XmlRoot(ElementName = "cockatrice_carddatabase")]
    public class CardDatabase
    {
        [XmlElement(ElementName = "sets")]
        public List<Sets> Sets { get; set; }
        [XmlElement(ElementName = "cards")]
        public List<Cards> Cards { get; set; }
    }

    [XmlRoot(ElementName = "sets")]
    public class Sets
    {
        [XmlElement(ElementName = "set")]
        public List<Set> Set { get; set; }
    }

    [XmlRoot(ElementName = "set")]
    public class Set
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "longname")]
        public string LongName { get; set; }
        [XmlElement(ElementName = "settype")]
        public string SetType { get; set; }
        [XmlElement(ElementName = "releasedate")]
        public string ReleaseDate { get; set; }
    }

    [XmlRoot(ElementName = "cards")]
    public class Cards
    {
        [XmlElement(ElementName = "card")]
        public List<Card> Card { get; set; }
    }

    [XmlRoot(ElementName = "card")]
    public class Card
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "set")]
        public CardSet[] CardSet { get; set; }
        [XmlElement(ElementName = "color")]
        public string Color { get; set; }
        [XmlElement(ElementName = "manacost")]
        public string ManaCost { get; set; }
        [XmlElement(ElementName = "cmc")]
        public string CMC { get; set; }
        [XmlElement(ElementName = "type")]
        public string Type { get; set; }
        [XmlElement(ElementName = "tablerow")]
        public string TableRow { get; set; }
        [XmlElement(ElementName = "text")]
        public string Text { get; set; }
        [XmlElement(ElementName = "loyalty")]
        public string Loyalty { get; set; }
        [XmlElement(ElementName = "pt")]
        public string PT { get; set; }
    }

    public class CardSet
    {
        [XmlAttribute(AttributeName = "rarity")]
        public string Rarity { get; set; }
        [XmlAttribute(AttributeName = "muId")]
        public string MuID { get; set; }
        [XmlAttribute(AttributeName = "num")]
        public string Num { get; set; }
        [XmlText]
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public string LongName { get; set; }
    }
}
