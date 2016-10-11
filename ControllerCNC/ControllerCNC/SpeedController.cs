﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

namespace ControllerCNC
{
    class SpeedController
    {
        private readonly DriverCNC _cnc;

        private readonly Thread _speedWorker;

        private volatile UInt16 _desiredDeltaT;

        private volatile UInt16 _currentDeltaT;

        private volatile bool _stop;


        private Queue<int> _plannedTimes = new Queue<int>();

        private int _plannedTimeTotal = 0;


        internal SpeedController(DriverCNC cnc)
        {
            _cnc = cnc;
            _desiredDeltaT = cnc.StartDeltaT;
            _currentDeltaT = _desiredDeltaT;
            _speedWorker = new Thread(worker);
            _speedWorker.IsBackground = true;
            _stop = true;
            _speedWorker.Start();
        }

        private void worker()
        {
            while (true)
            {
                if (!_stop)
                {

                    while (_cnc.IncompletePlanCount < _plannedTimes.Count)
                    {
                        _plannedTimeTotal -= _plannedTimes.Dequeue();
                    }

                    while (_plannedTimeTotal < 100 * 1000) //100
                        sendNewPlan();
                }
                Thread.Sleep(1);
            }
        }

        private void sendNewPlan()
        {
            if (_currentDeltaT != _desiredDeltaT)
                //TODO send acceleration
                _currentDeltaT = _desiredDeltaT;

            var stepCount = (Int16)(Math.Max(2, 10000 / _currentDeltaT));

            _cnc.SEND_Constant(stepCount, _currentDeltaT, 0);

            var plannedTime = stepCount * _currentDeltaT;
            _plannedTimeTotal += plannedTime;
            _plannedTimes.Enqueue(plannedTime);
        }

        internal void Start()
        {
            _stop = false;
        }

        internal void Stop()
        {
            _stop = true;
        }

        internal void SetRPM(int rpm)
        {
            var deltaT = _cnc.DeltaTFromRPM(rpm);
            if (deltaT > UInt16.MaxValue)
                throw new NotSupportedException("Value is out of range");
            _desiredDeltaT = (UInt16)deltaT;
        }
    }
}
