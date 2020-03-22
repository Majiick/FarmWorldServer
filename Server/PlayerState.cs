using LiteNetLib;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class PlayerState
        /*
         * This class is only for keeping state and veryfying correct state, no logic happens here.
         * 
         */
    {
        public struct MiningState
        {
            public string miningId;
            public DelayedEvent miningEndEventRef;
        }

        public struct FishingState
        {
            public DelayedEvent fishBitingEventRef;
            public ZonesSchema.Zone zone;
        }

        // Event states.
        public MiningState miningState = new MiningState();
        public FishingState fishingState = new FishingState();

        // State machine enums.
        private enum State { 
            Mining, 
            Fishing, 
                ThrowingBobbler,
                BobblerInWater,
                FishBiting,
            Neutral }
        private enum Trigger { 
            StartMining, AbortMining, EndMining, 
            ThrowBobbler, BobblerInWater, FishBite, FishSuccessfulCatch, FishUnsuccessfulCatch, AbortFishing }
        private StateMachine<State, Trigger> _stateMachine;

        // General information.
        public NetPeer _netPeer;
        public string _userName;
        public Packet.PlayerTransform lastTransform;

        public PlayerState(string userName, NetPeer netPeer)
        {
            _userName = userName;
            _netPeer = netPeer;

            ResetState();
        }

        public void ResetState()
        {
            miningState = new MiningState();
            _stateMachine = new StateMachine<State, Trigger>(State.Neutral);

            // Neutral
            _stateMachine.Configure(State.Neutral)
                .Permit(Trigger.StartMining, State.Mining)
                .Permit(Trigger.ThrowBobbler, State.ThrowingBobbler);

            // Mining
            _stateMachine.Configure(State.Mining)
                .Permit(Trigger.EndMining, State.Neutral)
                .Permit(Trigger.AbortMining, State.Neutral);

            // Fishing
            _stateMachine.Configure(State.Fishing)
                .Permit(Trigger.AbortFishing, State.Neutral);
            _stateMachine.Configure(State.ThrowingBobbler)
                .SubstateOf(State.Fishing)
                .Permit(Trigger.BobblerInWater, State.BobblerInWater);
            _stateMachine.Configure(State.BobblerInWater)
                .SubstateOf(State.Fishing)
                .Permit(Trigger.FishBite, State.FishBiting);
            _stateMachine.Configure(State.FishBiting)
                .SubstateOf(State.Fishing)
                .Permit(Trigger.FishSuccessfulCatch, State.Neutral)
                .Permit(Trigger.FishUnsuccessfulCatch, State.Neutral);
        }

        public void ThrowBobbler()
        {
            _stateMachine.Fire(Trigger.ThrowBobbler);
        }

        public void BobblerInWater(DelayedEvent e, ZonesSchema.Zone zone)
        {
            _stateMachine.Fire(Trigger.BobblerInWater);
            fishingState.fishBitingEventRef = e;
            fishingState.zone = zone;
        }

        public void FishBiting()
        {
            _stateMachine.Fire(Trigger.FishBite);
        }

        public void FishSuccessfulCatch()
        {
            _stateMachine.Fire(Trigger.FishSuccessfulCatch);
        }

        public void FishUnsuccessfulCatch()
        {
            _stateMachine.Fire(Trigger.FishUnsuccessfulCatch);
        }

        public void AbortFishing()
        {
            if (fishingState.fishBitingEventRef != null)
            {
                fishingState.fishBitingEventRef.Cancelled = true;
            }
            fishingState.zone = null;
            _stateMachine.Fire(Trigger.AbortFishing);
        }

        public void StartMining(string id, DelayedEvent e)
        {
            if (miningState.miningId != null)
            {
                Console.WriteLine(String.Format("Player {0} started mining object but their _miningId wasn't null, it was {1}.", _userName, miningState.miningId));
            }
            miningState.miningId = id;
            miningState.miningEndEventRef = e;
            _stateMachine.Fire(Trigger.StartMining);
        }

        public void AbortMining(string id)
        {
            if (miningState.miningId != id)
            {
                Console.WriteLine(String.Format("Player {0} tried to AbortMining on Object {1}, but they were mining object {2}", _userName, id, miningState.miningId));
            }
            miningState.miningId = null;
            miningState.miningEndEventRef = null;
            _stateMachine.Fire(Trigger.AbortMining);
        }

        public void EndMining(string id)
        {
            if (miningState.miningId != id)
            {
                Console.WriteLine(String.Format("Player {0} tried to EndMining on Object {1}, but they were mining object {2}", _userName, id, miningState.miningId));
            }

            miningState.miningId = null;
            miningState.miningEndEventRef = null;
            _stateMachine.Fire(Trigger.EndMining);
        }

        public bool IsMining(string id)
        {
            if (_stateMachine.IsInState(State.Mining) && miningState.miningId == null && miningState.miningEndEventRef == null)
            {
                Console.WriteLine(String.Format("State was mining but id or miningEndEventRef was null for player {0}", _userName));
            }

            if (_stateMachine.IsInState(State.Mining) && miningState.miningId == id)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public bool IsMining()
        {
            if (_stateMachine.IsInState(State.Mining) && miningState.miningId == null && miningState.miningEndEventRef == null)
            {
                Console.WriteLine(String.Format("State was mining but id or miningEndEventRef was null for player {0}", _userName));
            }
            return _stateMachine.IsInState(State.Mining);
        }
    }
}
