using System;

namespace ABShared.Protocol
{
    [Flags]
    public enum StatusCode
    {
        NeedUpdate  =   0x1,
        VersionOK   =   0x2,
        SingInOK    =   0x3,
        SingInFail  =   0x4,
        NeedSignIn  =   0x5,
        DataOK      =   0x6,
        HasEnd      =   0x7,
        EndRequest  =   0x8,
        LeftDays    =   0x9,
        SitesData   =   0x10,
        CloseConnection =   0x11
    }

    [Flags]
    public enum CommandCode
    {
        CheckVersion    =   0x21,
        SingIn          =   0x22,
        GetForks        =   0x23,
        GetLeftDay      =   0x24,
        GetSites        =   0x25,
        AddSiteData     =   0x26,
        Ping            =   0x27
    }
}
