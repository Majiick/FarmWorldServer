using LiteNetLib;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class PlayerState
    {
        private enum State { Mining, Neutral }
        private enum Trigger { StartMining, AbortMining, EndMining }
        private StateMachine<State, Trigger> _stateMachine;
        private string _miningId;

        public NetPeer _netPeer;
        public string _userName;
        public Packet.PlayerTransform lastTransform;

        public PlayerState(string userName, NetPeer netPeer)
        {
            _stateMachine = new StateMachine<State, Trigger>(State.Neutral);

            _stateMachine.Configure(State.Neutral)
                .Permit(Trigger.StartMining, State.Mining);
            _stateMachine.Configure(State.Mining)
                .Permit(Trigger.EndMining, State.Neutral)
                .Permit(Trigger.AbortMining, State.Neutral);

            _userName = userName;
            _netPeer = netPeer;
        }

        public void StartMining(string id)
        {
            _miningId = id;
            _stateMachine.Fire(Trigger.StartMining);
        }

        public void AbortMining()
        {
            _miningId = null;
            _stateMachine.Fire(Trigger.AbortMining);
        }

        public void EndMining()
        {
            _miningId = null;
            _stateMachine.Fire(Trigger.EndMining);
        }

        public bool IsMining(string id)
        {
            if (_stateMachine.IsInState(State.Mining) && _miningId == id)
            {
                return true;
            } else
            {
                return false;
            }
        }
    }
}
