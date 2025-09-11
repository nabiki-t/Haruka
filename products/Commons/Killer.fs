//=============================================================================
// Haruka Software Storage.
// Killer.fs : Defines HKiller class.
// HKiller class notice terminate request to objects.

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Collections.Immutable

//=============================================================================
// Type definition

[<NoComparison>]
type KillerRec =
    {
        /// Noticed flag.
        m_Flg : bool;

        /// A list of objects that notices terminate requests.
        m_SubObjs : ImmutableList< IComponent >;
    }

//=============================================================================
// Class implementation

/// HKiller class notice terminate request to objects.
type HKiller() = 

    /// Killer target object
    let m_Rec = OptimisticLock< KillerRec >( {
        m_Flg = false;
        m_SubObjs = ImmutableList.Empty
    })

    interface IKiller with

        // ----------------------------------------------------------------------------
        // Implementation of IKiller.Add
        override _.Add( o : IComponent ) : unit =
            m_Rec.Update ( fun oldRec ->
                if oldRec.m_Flg then
                    // If already notified, ignore update request.
                    oldRec
                else
                    {
                        oldRec with
                            m_SubObjs = oldRec.m_SubObjs.Add( o )
                    }
            )
            |> ignore

        // ----------------------------------------------------------------------------
        // Implementation of IKiller.NoticeTerminate
        override _.NoticeTerminate() : unit =
            let rl =
                m_Rec.Update ( fun oldRec ->
                    if oldRec.m_Flg then
                        // If already nitified, ignore duplicated termination request.
                        struct( oldRec, ImmutableList.Empty )
                    else
                        let newRec = {
                            m_Flg = true;
                            m_SubObjs = ImmutableList.Empty;
                        }
                        struct( newRec, oldRec.m_SubObjs )
                )
            for itr in rl do 
                itr.Terminate()

        // ----------------------------------------------------------------------------
        // Implementation of IKiller.IsNoticed
        override _.IsNoticed : bool =
            m_Rec.obj.m_Flg
