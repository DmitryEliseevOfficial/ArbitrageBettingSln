﻿// Generated by Xamasoft JSON Class Generator
// http://www.xamasoft.com/json-class-generator

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;


namespace ABServer.Parsers.fonbetModel
{

    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class Sport
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parentId")]
        public int ParentId { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("sortOrder")]
        public string SortOrder { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("regionId")]
        public int? RegionId { get; set; }

    }

    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class State
    {

        [JsonProperty("willBeLive")]
        public bool WillBeLive { get; set; }

        [JsonProperty("inHotList")]
        public bool? InHotList { get; set; }

        [JsonProperty("liveHalf")]
        public bool? LiveHalf { get; set; }

        [JsonProperty("webOnly")]
        public bool? WebOnly { get; set; }
    }


    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class Event
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("sortOrder")]
        public string SortOrder { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("num")]
        public int Num { get; set; }

        [JsonProperty("sportId")]
        public int SportId { get; set; }

        [JsonProperty("kind")]
        public int Kind { get; set; }

        [JsonProperty("rootKind")]
        public int RootKind { get; set; }

        [JsonProperty("state")]
        public State State { get; set; }

        [JsonProperty("team1")]
        public string Team1 { get; set; }

        [JsonProperty("team2")]
        public string Team2 { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("namePrefix")]
        public string NamePrefix { get; set; }

        [JsonProperty("startTime")]
        public int StartTime { get; set; }

        [JsonProperty("place")]
        public string Place { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("parentId")]
        public int? ParentId { get; set; }


        public int Team1Id { get; set; } = -1;
        public int Team2Id { get; set; } = -1;


        internal EventMisc EventMisc { get; set; }

        internal Dictionary<int, CustomFactor> Factors { get; set; }
        internal bool IsBlock { get; set; }
    }

    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class EventBlock
    {

        [JsonProperty("eventId")]
        public int EventId { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("factors")]
        public List<int> Factors { get; set; }
    }

    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class EventMisc
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("liveDelay")]
        public int LiveDelay { get; set; }

        [JsonProperty("score1")]
        public int Score1 { get; set; }

        [JsonProperty("score2")]
        public int Score2 { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("servingTeam")]
        public int? ServingTeam { get; set; }

        [JsonProperty("tv")]
        public List<int> Tv { get; set; }

        [JsonProperty("timerDirection")]
        public int? TimerDirection { get; set; }

        [JsonProperty("timerSeconds")]
        public int? TimerSeconds { get; set; }

        [JsonProperty("timerUpdateTimestamp")]
        public int? TimerUpdateTimestamp { get; set; }

    }

    [DebuggerDisplay("EvId:{E} Factor:{F} Value:{V} IsLIve:{IsLive}")]
    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class CustomFactor
    {

        [JsonProperty("e")]
        public int E { get; set; }

        [JsonProperty("f")]
        public int F { get; set; }

        [JsonProperty("v")]
        public float V { get; set; }

        [JsonProperty("isLive")]
        public bool IsLive { get; set; }

        [JsonProperty("p")]
        public int? P { get; set; }

        [JsonProperty("pt")]
        public string Pt { get; set; }

        [JsonProperty("lo")]
        public int? Lo { get; set; }

        [JsonProperty("hi")]
        public int? Hi { get; set; }

        public bool IsBlocked { get; set; }
    }

    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class Announcement
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("num")]
        public int Num { get; set; }

        [JsonProperty("segmentId")]
        public int SegmentId { get; set; }

        [JsonProperty("segmentName")]
        public string SegmentName { get; set; }

        [JsonProperty("sportId")]
        public int SportId { get; set; }

        [JsonProperty("segmentSortOrder")]
        public string SegmentSortOrder { get; set; }

        [JsonProperty("team1")]
        public string Team1 { get; set; }

        [JsonProperty("team2")]
        public string Team2 { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("namePrefix")]
        public string NamePrefix { get; set; }

        [JsonProperty("startTime")]
        public int StartTime { get; set; }

        [JsonProperty("place")]
        public string Place { get; set; }

        [JsonProperty("regionId")]
        public int? RegionId { get; set; }

        [JsonProperty("liveHalf")]
        public bool? LiveHalf { get; set; }

        [JsonProperty("tv")]
        public List<int> Tv { get; set; }
    }



    [Obfuscation(Feature = "trigger", Exclude = false)]
    internal class FonbetResponse
    {
        [JsonProperty("packetVersion")]
        public int PacketVersion { get; set; }

        [JsonProperty("fromVersion")]
        public int FromVersion { get; set; }

        [JsonProperty("factorsVersion")]
        public int FactorsVersion { get; set; }

        [JsonProperty("siteVersion")]
        public int SiteVersion { get; set; }

        [JsonProperty("sports")]
        public List<Sport> Sports { get; set; }


        [JsonProperty("events")]
        public List<Event> Events { get; set; }

        [JsonProperty("eventBlocks")]
        public List<EventBlock> EventBlocks { get; set; }

        [JsonProperty("eventMiscs")]
        public List<EventMisc> EventMiscs { get; set; }

        [JsonProperty("customFactors")]
        public List<CustomFactor> CustomFactors { get; set; }

        [JsonProperty("announcements")]
        public List<Announcement> Announcements { get; set; }
    }
}
