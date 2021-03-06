﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HidLibrary;

namespace Octopode {
    public class CommandController {
        public Queue<byte[]> commands;
        private HidDevice context;
        private object lockObject;
        private Task runningTask;
        private long commandId;
        private long resolvedCommandId;

        public Action<long> ResolvedCommand;
        
        
        public CommandController(HidDevice device) {
            lockObject = new object();
            context = device;
            commands = new Queue<byte[]>();
        }

        public long AddCommand(byte[] command) {
            lock(lockObject) {
                commands.Enqueue(command);
                CheckAndStart();
                return ++commandId;
            }
        }

        private void CheckAndStart() {
            if(runningTask != null && !runningTask.IsCompleted) {
                return;
            }

            runningTask = Task.Run(() => CommandLoop());
        }

        private void CommandLoop() {
            while(commands.Count > 0) {
                DispatchCommand(commands.Dequeue());
            }
        }

        private void DispatchCommand(byte[] command) {
            if(!context.Write(command, 100)) {
                Console.Error.WriteLine("A given command did not finish in the given time.");
            }
            ResolvedCommand.Invoke(++resolvedCommandId);
        }
    }
}