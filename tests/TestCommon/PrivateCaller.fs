//=============================================================================
// Haruka Software Storage.
// PrivateCaller.fs : Defines PrivateCaller classe for debug or test use.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System.Reflection

//=============================================================================
// Class implementation

type PrivateCaller( o : obj ) =
    let m_Obj = o

    static member Invoke< 'a >( name : string, args : obj [] ) : obj =
        let r =
            typeof< 'a >.GetRuntimeMethods()
            |> Seq.tryFind ( fun itr -> itr.Name = name ) 
        match r with
        | None ->
            raise <| System.Exception( "Method name not found." )
        | Some( i ) ->
            try
                i.Invoke( null, args )
            with
            | _ as x ->
                x.InnerException |> raise

    static member Invoke< 'a >( name : string ) : obj =
        PrivateCaller.Invoke< 'a >( name, Array.empty )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6; a7 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj,
            a11 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10; a11 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj,
            a11 : obj,
            a12 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10; a11; a12 |] )

    static member Invoke< 'a >
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj,
            a11 : obj,
            a12 : obj,
            a13 : obj
        ) : obj =
            PrivateCaller.Invoke< 'a >( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10; a11; a12; a13 |] )


    member public _.GetField( name : string ) : obj =
        try
            let invokeAttr = BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.GetField ||| BindingFlags.NonPublic
            m_Obj.GetType().InvokeMember( name, invokeAttr, null, m_Obj, Array.empty )
        with
        | _ as x ->
            if x.InnerException <> null then
                x.InnerException |> raise
            else
                x |> raise

    member public _.SetField( name : string, o : obj ) : unit =
        try
            let invokeAttr = BindingFlags.Instance ||| BindingFlags.GetField ||| BindingFlags.NonPublic
            m_Obj.GetType().GetField( name, invokeAttr ).SetValue( m_Obj, o )
        with
        | _ as x ->
            if x.InnerException <> null then
                x.InnerException |> raise
            else
                x |> raise

    member public _.Invoke( name : string, args : obj [] ) : obj =
        try
            let invokeAttr = BindingFlags.InvokeMethod ||| BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.NonPublic
            m_Obj.GetType().InvokeMember( name, invokeAttr, null, m_Obj, args )
        with
        | _ as x ->
            if x.InnerException <> null then
                x.InnerException |> raise
            else
                x |> raise

    member public this.Invoke ( name : string ) : obj =
        this.Invoke( name, Array.empty )

    member public this.Invoke
        (   name : string,
            a0 : obj
        ) : obj =
            this.Invoke( name, [| a0 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6; a7 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj,
            a11 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10; a11 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj,
            a11 : obj,
            a12 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10; a11; a12 |] )

    member public this.Invoke
        (   name : string,
            a0 : obj,
            a1 : obj,
            a2 : obj,
            a3 : obj,
            a4 : obj,
            a5 : obj,
            a6 : obj,
            a7 : obj,
            a8 : obj,
            a9 : obj,
            a10 : obj,
            a11 : obj,
            a12 : obj,
            a13 : obj
        ) : obj =
            this.Invoke( name, [| a0; a1; a2; a3; a4; a5; a6; a7; a8; a9; a10; a11; a12; a13 |] )

