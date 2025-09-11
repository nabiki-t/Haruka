//=============================================================================
// Haruka Software Storage.
// StringTableTests.fs : Test cases for StringTable class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Reflection
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

type StringTable_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    static let m_ResReaderLock = new SemaphoreSlim(1)

    static let resourceDirName =
        let s = Assembly.GetEntryAssembly()
        Functions.AppendPathName ( Path.GetDirectoryName s.Location ) "Resource"

    static let updateResourceFile( lang : string ) =
        let orgFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s.xml" lang )
        let bkFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s.xml_bk" lang )
        let testFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s_test.xml" lang )
        if File.Exists orgFileName then
            File.Move( orgFileName, bkFileName )
        File.Move ( testFileName, orgFileName )

    static let hideResourceFile( lang : string ) =
        let orgFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s.xml" lang )
        let bkFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s.xml_bk" lang )
        if File.Exists orgFileName then
            File.Move( orgFileName, bkFileName )

    static let revertResourceFile( lang : string ) =
        let orgFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s.xml" lang )
        let bkFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s.xml_bk" lang )
        let testFileName = Functions.AppendPathName resourceDirName ( sprintf "Messages_%s_test.xml" lang )
        if File.Exists orgFileName then
            File.Move ( orgFileName, testFileName )
        if File.Exists bkFileName then
            File.Move( bkFileName, orgFileName )

    static member public ReadDefaultResource() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore
        st

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "es-ES" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read default en-US resource
        let m = st.GetMessage( "TEST_MESSAGE_0" )
        Assert.True(( m = "TEST_MESSAGE" ))

    [<Fact>]
    member _.Constractor_002() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "es-ES" )
        updateResourceFile "es-ES"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "es-ES"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read es-ES resource
        let m = st.GetMessage( "TEST_MESSAGE_0" )
        Assert.True(( m = "TEST_MESSAGE_es" ))

    [<Fact>]
    member _.Constractor_003() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m = st.GetMessage( "TEST_MESSAGE_0" )
        Assert.True(( m = "TEST_MESSAGE" ))

    [<Fact>]
    member _.Constractor_004() =

        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        hideResourceFile "en-US"

        try
            new StringTable( Constants.MESSAGE_RESX_FILE_NAME ) |> ignore
        with
        | _ ->
            Assert.Fail __LINE__
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

    [<Fact>]
    member _.Constractor_005() =

        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let s = new StringTable( "Test001" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let msg1 = s.Get( "", "MSG001" )
        Assert.True(( msg1 = "TEST_MESSAGE_001" || msg1 = "TEST_MESSAGE_002" ))

        let msg2 = s.Get( "", "MSG002" )
        Assert.True(( msg2 = "TEST_MESSAGE_003" ))

        let msg2 = s.Get( "", "MSG002" )
        Assert.True(( msg2 = "TEST_MESSAGE_003" ))

        let msg3 = s.Get( "SECTION001", "MSG001" )
        Assert.True(( msg3 = "TEST_MESSAGE_001_001" || msg3 = "TEST_MESSAGE_001_002" ))

        let msg4 = s.Get( "SECTION001", "MSG002" )
        Assert.True(( msg4 = "TEST_MESSAGE_002_001" ))


    [<Fact>]
    member _.GetMessage_000() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.GetMessage( "TEST_MESSAGE_0" )
        Assert.True(( m0 = "TEST_MESSAGE" ))

        let m1 = st.GetMessage( "TEST_MESSAGE_0", "A" )
        Assert.True(( m1 = "TEST_MESSAGE" ))

        let m2 = st.GetMessage( "TEST_MESSAGE_0", "B", "C" )
        Assert.True(( m2 = "TEST_MESSAGE" ))

        let m3 = st.GetMessage( "TEST_MESSAGE_0", "B", "C", "D" )
        Assert.True(( m3 = "TEST_MESSAGE" ))

        let m4 = st.GetMessage( "TEST_MESSAGE_0", "B", "C", "D", "E" )
        Assert.True(( m4 = "TEST_MESSAGE" ))

        let m5 = st.GetMessage( "TEST_MESSAGE_0", "B", "C", "D", "E", "F" )
        Assert.True(( m5 = "TEST_MESSAGE" ))

    [<Fact>]
    member _.GetMessage_001() =

        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.GetMessage( "TEST_MESSAGE_1" )
        Assert.True(( m0 = "TEST_MESSAGE_" ))

        let m1 = st.GetMessage( "TEST_MESSAGE_1", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A" ))

        let m2 = st.GetMessage( "TEST_MESSAGE_1", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A" ))

        let m3 = st.GetMessage( "TEST_MESSAGE_1", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A" ))

        let m4 = st.GetMessage( "TEST_MESSAGE_1", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A" ))

        let m5 = st.GetMessage( "TEST_MESSAGE_1", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A" ))

    [<Fact>]
    member _.GetMessage_002() =

        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.GetMessage( "TEST_MESSAGE_2" )
        Assert.True(( m0 = "TEST_MESSAGE__" ))

        let m1 = st.GetMessage( "TEST_MESSAGE_2", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A_" ))

        let m2 = st.GetMessage( "TEST_MESSAGE_2", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B" ))

        let m3 = st.GetMessage( "TEST_MESSAGE_2", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B" ))

        let m4 = st.GetMessage( "TEST_MESSAGE_2", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B" ))

        let m5 = st.GetMessage( "TEST_MESSAGE_2", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B" ))

    [<Fact>]
    member _.GetMessage_003() =

        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.GetMessage( "TEST_MESSAGE_3" )
        Assert.True(( m0 = "TEST_MESSAGE___" ))

        let m1 = st.GetMessage( "TEST_MESSAGE_3", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A__" ))

        let m2 = st.GetMessage( "TEST_MESSAGE_3", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B_" ))

        let m3 = st.GetMessage( "TEST_MESSAGE_3", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B_C" ))

        let m4 = st.GetMessage( "TEST_MESSAGE_3", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B_C" ))

        let m5 = st.GetMessage( "TEST_MESSAGE_3", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B_C" ))

    [<Fact>]
    member _.GetMessage_004() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.GetMessage( "TEST_MESSAGE_4" )
        Assert.True(( m0 = "TEST_MESSAGE____" ))

        let m1 = st.GetMessage( "TEST_MESSAGE_4", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A___" ))

        let m2 = st.GetMessage( "TEST_MESSAGE_4", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B__" ))

        let m3 = st.GetMessage( "TEST_MESSAGE_4", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B_C_" ))

        let m4 = st.GetMessage( "TEST_MESSAGE_4", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B_C_D" ))

        let m5 = st.GetMessage( "TEST_MESSAGE_4", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B_C_D" ))

    [<Fact>]
    member _.GetMessage_005() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let m0 = st.GetMessage( "TEST_MESSAGE_5" )
        Assert.True(( m0 = "TEST_MESSAGE_____" ))

        let m1 = st.GetMessage( "TEST_MESSAGE_5", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A____" ))

        let m2 = st.GetMessage( "TEST_MESSAGE_5", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B___" ))

        let m3 = st.GetMessage( "TEST_MESSAGE_5", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B_C__" ))

        let m4 = st.GetMessage( "TEST_MESSAGE_5", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B_C_D_" ))

        let m5 = st.GetMessage( "TEST_MESSAGE_5", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B_C_D_E" ))

    [<Fact>]
    member _.GetMessage_006() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        updateResourceFile "en-US"
        let st = new StringTable( Constants.MESSAGE_RESX_FILE_NAME )
        revertResourceFile "en-US"
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.GetMessage( "123" )
        Assert.True(( m0 = "Unknown message section '' name '123' was specified. Arguments=" ))

        let m1 = st.GetMessage( "123", "A" )
        Assert.True(( m1 = "Unknown message section '' name '123' was specified. Arguments=A" ))

        let m2 = st.GetMessage( "123", "A", "B" )
        Assert.True(( m2 = "Unknown message section '' name '123' was specified. Arguments=A, B" ))

        let m3 = st.GetMessage( "123", "A", "B", "C" )
        Assert.True(( m3 = "Unknown message section '' name '123' was specified. Arguments=A, B, C" ))

        let m4 = st.GetMessage( "123", "A", "B", "C", "D" )
        Assert.True(( m4 = "Unknown message section '' name '123' was specified. Arguments=A, B, C, D" ))

        let m5 = st.GetMessage( "123", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "Unknown message section '' name '123' was specified. Arguments=A, B, C, D, E" ))

    [<Fact>]
    member _.GetMessage_007() =
        let st = new StringTable( "" )

        let m0 = st.GetMessage( "AAA" )
        Assert.True(( m0 = "AAA : " ))

        let m1 = st.GetMessage( "AAA", "A" )
        Assert.True(( m1 = "AAA : A" ))

        let m2 = st.GetMessage( "AAA", "A", "B" )
        Assert.True(( m2 = "AAA : A, B" ))

        let m3 = st.GetMessage( "AAA", "A", "B", "C" )
        Assert.True(( m3 = "AAA : A, B, C" ))

        let m4 = st.GetMessage( "AAA", "A", "B", "C", "D" )
        Assert.True(( m4 = "AAA : A, B, C, D" ))

        let m5 = st.GetMessage( "AAA", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "AAA : A, B, C, D, E" ))

    [<Fact>]
    member _.Get_000() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let m0 = st.Get( "SECTION001", "TEST_MESSAGE_0" )
        Assert.True(( m0 = "TEST_MESSAGE" ))

        let m1 = st.Get( "SECTION001", "TEST_MESSAGE_0", "A" )
        Assert.True(( m1 = "TEST_MESSAGE" ))

        let m2 = st.Get( "SECTION001", "TEST_MESSAGE_0", "B", "C" )
        Assert.True(( m2 = "TEST_MESSAGE" ))

        let m3 = st.Get( "SECTION001", "TEST_MESSAGE_0", "B", "C", "D" )
        Assert.True(( m3 = "TEST_MESSAGE" ))

        let m4 = st.Get( "SECTION001", "TEST_MESSAGE_0", "B", "C", "D", "E" )
        Assert.True(( m4 = "TEST_MESSAGE" ))

        let m5 = st.Get( "SECTION001", "TEST_MESSAGE_0", "B", "C", "D", "E", "F" )
        Assert.True(( m5 = "TEST_MESSAGE" ))

    [<Fact>]
    member _.Get_001() =

        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let m0 = st.Get( "SECTION001", "TEST_MESSAGE_1" )
        Assert.True(( m0 = "TEST_MESSAGE_" ))

        let m1 = st.Get( "SECTION001", "TEST_MESSAGE_1", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A" ))

        let m2 = st.Get( "SECTION001", "TEST_MESSAGE_1", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A" ))

        let m3 = st.Get( "SECTION001", "TEST_MESSAGE_1", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A" ))

        let m4 = st.Get( "SECTION001", "TEST_MESSAGE_1", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A" ))

        let m5 = st.Get( "SECTION001", "TEST_MESSAGE_1", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A" ))

    [<Fact>]
    member _.Get_002() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let m0 = st.Get( "SECTION001", "TEST_MESSAGE_2" )
        Assert.True(( m0 = "TEST_MESSAGE__" ))

        let m1 = st.Get( "SECTION001", "TEST_MESSAGE_2", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A_" ))

        let m2 = st.Get( "SECTION001", "TEST_MESSAGE_2", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B" ))

        let m3 = st.Get( "SECTION001", "TEST_MESSAGE_2", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B" ))

        let m4 = st.Get( "SECTION001", "TEST_MESSAGE_2", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B" ))

        let m5 = st.Get( "SECTION001", "TEST_MESSAGE_2", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B" ))

    [<Fact>]
    member _.Get_003() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let m0 = st.Get( "SECTION001", "TEST_MESSAGE_3" )
        Assert.True(( m0 = "TEST_MESSAGE___" ))

        let m1 = st.Get( "SECTION001", "TEST_MESSAGE_3", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A__" ))

        let m2 = st.Get( "SECTION001", "TEST_MESSAGE_3", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B_" ))

        let m3 = st.Get( "SECTION001", "TEST_MESSAGE_3", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B_C" ))

        let m4 = st.Get( "SECTION001", "TEST_MESSAGE_3", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B_C" ))

        let m5 = st.Get( "SECTION001", "TEST_MESSAGE_3", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B_C" ))

    [<Fact>]
    member _.Get_004() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let m0 = st.Get( "SECTION001", "TEST_MESSAGE_4" )
        Assert.True(( m0 = "TEST_MESSAGE____" ))

        let m1 = st.Get( "SECTION001", "TEST_MESSAGE_4", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A___" ))

        let m2 = st.Get( "SECTION001", "TEST_MESSAGE_4", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B__" ))

        let m3 = st.Get( "SECTION001", "TEST_MESSAGE_4", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B_C_" ))

        let m4 = st.Get( "SECTION001", "TEST_MESSAGE_4", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B_C_D" ))

        let m5 = st.Get( "SECTION001", "TEST_MESSAGE_4", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B_C_D" ))

    [<Fact>]
    member _.Get_005() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let m0 = st.Get( "SECTION001", "TEST_MESSAGE_5" )
        Assert.True(( m0 = "TEST_MESSAGE_____" ))

        let m1 = st.Get( "SECTION001", "TEST_MESSAGE_5", "A" )
        Assert.True(( m1 = "TEST_MESSAGE_A____" ))

        let m2 = st.Get( "SECTION001", "TEST_MESSAGE_5", "A", "B" )
        Assert.True(( m2 = "TEST_MESSAGE_A_B___" ))

        let m3 = st.Get( "SECTION001", "TEST_MESSAGE_5", "A", "B", "C" )
        Assert.True(( m3 = "TEST_MESSAGE_A_B_C__" ))

        let m4 = st.Get( "SECTION001", "TEST_MESSAGE_5", "A", "B", "C", "D" )
        Assert.True(( m4 = "TEST_MESSAGE_A_B_C_D_" ))

        let m5 = st.Get( "SECTION001", "TEST_MESSAGE_5", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "TEST_MESSAGE_A_B_C_D_E" ))

    [<Fact>]
    member _.Get_006() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.Get( "SECTION001", "123" )
        Assert.True(( m0 = "Unknown message section 'SECTION001' name '123' was specified. Arguments=" ))

        let m1 = st.Get( "SECTION001", "123", "A" )
        Assert.True(( m1 = "Unknown message section 'SECTION001' name '123' was specified. Arguments=A" ))

        let m2 = st.Get( "SECTION001", "123", "A", "B" )
        Assert.True(( m2 = "Unknown message section 'SECTION001' name '123' was specified. Arguments=A, B" ))

        let m3 = st.Get( "SECTION001", "123", "A", "B", "C" )
        Assert.True(( m3 = "Unknown message section 'SECTION001' name '123' was specified. Arguments=A, B, C" ))

        let m4 = st.Get( "SECTION001", "123", "A", "B", "C", "D" )
        Assert.True(( m4 = "Unknown message section 'SECTION001' name '123' was specified. Arguments=A, B, C, D" ))

        let m5 = st.Get( "SECTION001", "123", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "Unknown message section 'SECTION001' name '123' was specified. Arguments=A, B, C, D, E" ))

    [<Fact>]
    member _.Get_007() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test002" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        // read en-US resource
        let m0 = st.Get( "aaa", "TEST_MESSAGE_5" )
        Assert.True(( m0 = "Unknown message section 'aaa' name 'TEST_MESSAGE_5' was specified. Arguments=" ))

        let m1 = st.Get( "aaa", "TEST_MESSAGE_5", "A" )
        Assert.True(( m1 = "Unknown message section 'aaa' name 'TEST_MESSAGE_5' was specified. Arguments=A" ))

        let m2 = st.Get( "aaa", "TEST_MESSAGE_5", "A", "B" )
        Assert.True(( m2 = "Unknown message section 'aaa' name 'TEST_MESSAGE_5' was specified. Arguments=A, B" ))

        let m3 = st.Get( "aaa", "TEST_MESSAGE_5", "A", "B", "C" )
        Assert.True(( m3 = "Unknown message section 'aaa' name 'TEST_MESSAGE_5' was specified. Arguments=A, B, C" ))

        let m4 = st.Get( "aaa", "TEST_MESSAGE_5", "A", "B", "C", "D" )
        Assert.True(( m4 = "Unknown message section 'aaa' name 'TEST_MESSAGE_5' was specified. Arguments=A, B, C, D" ))

        let m5 = st.Get( "aaa", "TEST_MESSAGE_5", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "Unknown message section 'aaa' name 'TEST_MESSAGE_5' was specified. Arguments=A, B, C, D, E" ))

    [<Fact>]
    member _.Get_008() =
        let st = new StringTable( "" )

        let m0 = st.Get( "SECTION001", "AAA" )
        Assert.True(( m0 = "AAA : " ))

        let m1 = st.Get( "SECTION001", "AAA", "A" )
        Assert.True(( m1 = "AAA : A" ))

        let m2 = st.Get( "SECTION001", "AAA", "A", "B" )
        Assert.True(( m2 = "AAA : A, B" ))

        let m3 = st.Get( "SECTION001", "AAA", "A", "B", "C" )
        Assert.True(( m3 = "AAA : A, B, C" ))

        let m4 = st.Get( "SECTION001", "AAA", "A", "B", "C", "D" )
        Assert.True(( m4 = "AAA : A, B, C, D" ))

        let m5 = st.Get( "SECTION001", "AAA", "A", "B", "C", "D", "E" )
        Assert.True(( m5 = "AAA : A, B, C, D, E" ))

    [<Fact>]
    member _.GetSectionNames_001() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test003" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let secNames = st.GetSectionNames()
        Assert.True(( Seq.length secNames = 3 ))
        Assert.True(( Seq.exists ( (=) "SECTION001" ) secNames ))
        Assert.True(( Seq.exists ( (=) "SECTION002" ) secNames ))
        Assert.True(( Seq.exists ( (=) "" ) secNames ))

    [<Fact>]
    member _.GetSectionNames_002() =
        let st = new StringTable( "" )
        let secNames = st.GetSectionNames()
        Assert.True(( Seq.length secNames = 0 ))

    [<Fact>]
    member _.GetNames_001() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test003" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let keyNames = st.GetNames ""
        Assert.True(( Seq.length keyNames = 2 ))
        Assert.True(( Seq.exists ( (=) "KEY_0_1" ) keyNames ))
        Assert.True(( Seq.exists ( (=) "KEY_0_2" ) keyNames ))

    [<Fact>]
    member _.GetNames_002() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test003" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let keyNames = st.GetNames "SECTION001"
        Assert.True(( Seq.length keyNames = 2 ))
        Assert.True(( Seq.exists ( (=) "KEY_1_1" ) keyNames ))
        Assert.True(( Seq.exists ( (=) "KEY_1_2" ) keyNames ))

    [<Fact>]
    member _.GetNames_003() =
        m_ResReaderLock.Wait()
        let culbk = Thread.CurrentThread.CurrentCulture
        Thread.CurrentThread.CurrentCulture <- Globalization.CultureInfo( "en-US" )
        let st = new StringTable( "Test003" )
        Thread.CurrentThread.CurrentCulture <- culbk
        m_ResReaderLock.Release() |> ignore

        let keyNames = st.GetNames "aaaa"
        Assert.True(( Seq.length keyNames = 0 ))

    [<Fact>]
    member _.GetNames_004() =
        let st = new StringTable( "" )
        let keyNames = st.GetNames "aaaa"
        Assert.True(( Seq.length keyNames = 0 ))
