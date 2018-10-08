﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Lidgren.Network.Encryption;

namespace Lidgren.Network.ContractCommunication
{
    public abstract class CommunicatorBase<TServiceContract> where TServiceContract : IContract,new()
    {

        private Dictionary<ushort, MessageFilter> _recieveFilters { get;  set; }
        private Dictionary<ushort, MessageFilter> _sendFilters { get;  set; }
        protected Dictionary<string, ushort> _sendAddressDictionary { get; private set; }
        private readonly Dictionary<string, AwaitingCallJob> _awaitingCalls = new Dictionary<string, AwaitingCallJob>();
        private readonly List<TaskJob> _runningTasks = new List<TaskJob>();
        public int CurrentTaskCount => _runningTasks.Count;
        public TServiceContract Contract { get; private set; }

        protected NetPeer NetConnector;
        protected NetPeerConfiguration Configuration;
        protected CommunicationSettings CommunicationSettings = new CommunicationSettings();
        public NetConnectionStatus ConnectionStatus { get; protected set; }
        
        protected ConverterBase Converter;
        public INetEncryptor NetEncryptor { get; protected set; }
        public event Action<NetConnectionStatus,NetConnectionResult> OnConnectionStatusChangedEvent;
        private List<Tuple<Object, string>> _logList = new List<Tuple<object, string>>();
        private event Action<object, string> _onLoggedEvent;
        public event Action<object, string> OnLoggedEvent
        {
            add
            {
                _onLoggedEvent += value;
                foreach (var logEntry in _logList)
                {
                    value.Invoke(logEntry.Item1,logEntry.Item2);
                }
            }
            remove => _onLoggedEvent -= value;
        }

        public List<Exception> ExceptionsCaught { get; private set; } = new List<Exception>();

        protected void Initialize(Type sendContractType, Type recieveContractType)
        {
            if (!sendContractType.IsInterface || !recieveContractType.IsInterface)
            {
                throw new Exception("type must be an interface!");
            }

            Contract = new TServiceContract();
            _recieveFilters = MapContract(this, recieveContractType);
            _sendFilters = MapContract(Contract, sendContractType);
            var sendContract = Contract.GetType().GetInterfaces().First(@interface =>
                sendContractType.IsAssignableFrom(@interface) && @interface != sendContractType);
            _sendAddressDictionary = GetAddresses(sendContract);
        }
        private Dictionary<string, ushort> GetAddresses(Type type)
        {
            var addresses = new Dictionary<string, ushort>();
            var methods = type.GetMethods();
            var addressIndexer = default(ushort);
            foreach (var methodInfo in methods)
            {
                addresses.Add(methodInfo.Name, addressIndexer++);
            }
            Log($"Got {addresses.Count} addresses for {type.Name}");
            return addresses;
        }
        private Dictionary<ushort, MessageFilter> MapContract(object mapObject, Type inheritedType)
        {
            var interfaces = mapObject.GetType().GetInterfaces();
            var contract = interfaces.First(@interface => inheritedType.IsAssignableFrom(@interface) && @interface != inheritedType);
            var methods = new List<MethodInfo>(contract.GetMethods(/*flags*/).OrderByDescending(m => m.Name));

            var addresses = GetAddresses(contract);
            var callbackFilters = new Dictionary<ushort, MessageFilter>();

            foreach (var methodInfo in methods)
            {
                var messageFilter = new MessageFilter();
                messageFilter.Method = methodInfo;
                messageFilter.Types = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
                callbackFilters.Add(addresses[methodInfo.Name], messageFilter);
            }
            return callbackFilters;
        }
        public void Call(Action method, NetConnection connection = null) => CreateAndCall(method.Method, null, connection);
        public void Call<T1>(Action<T1> method, T1 arg1, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1 }, connection);
        public void Call<T1, T2>(Action<T1, T2> method, T1 arg1, T2 arg2, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2 }, connection);
        public void Call<T1, T2, T3>(Action<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3 }, connection);
        public void Call<T1, T2, T3, T4>(Action<T1, T2, T3,T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3, arg4 }, connection);
        public void Call<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4,T5 arg5, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3, arg4, arg5 }, connection);
        public void Call<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4,T5 arg5,T6 arg6, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6 }, connection);
        public void Call<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4,T5 arg5,T6 arg6, T7 arg7, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 }, connection);
        public void Call<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4,T5 arg5,T6 arg6, T7 arg7, T8 arg8, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }, connection);
        public void Call<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4,T5 arg5,T6 arg6, T7 arg7, T8 arg8, T9 arg9, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 }, connection);
        public void Call<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4,T5 arg5,T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 }, connection);
        //default last parameter netConnection
        public void Call(Action<NetConnection> method, NetConnection connection = null) => CreateAndCall(method.Method, null, connection);
        public void Call<T1>(Action<T1, NetConnection> method, T1 arg1, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1 }, connection);
        public void Call<T1,T2>(Action<T1,T2, NetConnection> method, T1 arg1,T2 arg2, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2 }, connection);
        public void Call<T1,T2,T3>(Action<T1,T2,T3, NetConnection> method, T1 arg1,T2 arg2,T3 arg3, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3 }, connection);
        public void Call<T1,T2,T3,T4>(Action<T1,T2,T3,T4, NetConnection> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3, arg4 }, connection);
        public void Call<T1,T2,T3,T4,T5>(Action<T1,T2,T3,T4,T5, NetConnection> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3, arg4, arg5 }, connection);
        public void Call<T1,T2,T3,T4,T5,T6>(Action<T1,T2,T3,T4,T5,T6, NetConnection> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, T6 arg6, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3, arg4, arg5, arg6 }, connection);
        public void Call<T1,T2,T3,T4,T5,T6,T7>(Action<T1,T2,T3,T4,T5,T6,T7, NetConnection> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, T6 arg6, T7 arg7, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3, arg4, arg5, arg6, arg7 }, connection);
        public void Call<T1,T2,T3,T4,T5,T6,T7,T8>(Action<T1,T2,T3,T4,T5,T6,T7,T8, NetConnection> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, T6 arg6, T7 arg7,T8 arg8, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3, arg4, arg5, arg6, arg7, arg8 }, connection);
        public void Call<T1,T2,T3,T4,T5,T6,T7,T8,T9>(Action<T1,T2,T3,T4,T5,T6,T7,T8,T9, NetConnection> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, T6 arg6, T7 arg7,T8 arg8, T9 arg9, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 }, connection);
        public void Call<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(Action<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, NetConnection> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, T6 arg6, T7 arg7,T8 arg8, T9 arg9, T10 arg10, NetConnection connection = null) => CreateAndCall(method.Method, new object[] { arg1,arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 }, connection);
        //Task with default last parameter netConnection
        public void Call<T1>(Func<T1, NetConnection, Task> method, T1 arg1, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1});
        public void Call<T1,T2>(Func<T1,T2, NetConnection, Task> method, T1 arg1,T2 arg2, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2});
        public void Call<T1,T2,T3>(Func<T1,T2,T3, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3});
        public void Call<T1,T2,T3,T4>(Func<T1,T2,T3,T4, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3,arg4});
        public void Call<T1,T2,T3,T4,T5>(Func<T1,T2,T3,T4,T5, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3,arg4,arg5});
        public void Call<T1,T2,T3,T4,T5,T6>(Func<T1,T2,T3,T4,T5,T6, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3,arg4,arg5,arg6});
        public void Call<T1,T2,T3,T4,T5,T6,T7>(Func<T1,T2,T3,T4,T5,T6,T7, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3,arg4,arg5,arg6,arg7});
        public void Call<T1,T2,T3,T4,T5,T6,T7,T8>(Func<T1,T2,T3,T4,T5,T6,T7,T8, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7,T8 arg8, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8});
        public void Call<T1,T2,T3,T4,T5,T6,T7,T8,T9>(Func<T1,T2,T3,T4,T5,T6,T7,T8,T9, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7,T8 arg8,T9 arg9, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9});
        public void Call<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(Func<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, NetConnection, Task> method, T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7,T8 arg8,T9 arg9,T10 arg10, NetConnection connection = null) => CreateAndCall(method.Method, new object[] {arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10});

        public Task<TReturn> CallAsync<TReturn>(Func<NetConnection, Task<TReturn>> method, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,null,connection);
        public Task<TReturn> CallAsync<T1,TReturn>(Func<T1, NetConnection, Task<TReturn>> method,T1 arg1, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1},connection);
        public Task<TReturn> CallAsync<T1,T2,TReturn>(Func<T1,T2, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,TReturn>(Func<T1,T2,T3, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,T4,TReturn>(Func<T1,T2,T3,T4, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3,T4 arg4, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3,arg4},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,T4,T5,TReturn>(Func<T1,T2,T3,T4,T5, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3,arg4,arg5},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,T4,T5,T6,TReturn>(Func<T1,T2,T3,T4,T5,T6, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3,arg4,arg5,arg6},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,T4,T5,T6,T7,TReturn>(Func<T1,T2,T3,T4,T5,T6,T7, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3,arg4,arg5,arg6,arg7},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,T4,T5,T6,T7,T8,TReturn>(Func<T1,T2,T3,T4,T5,T6,T7,T8, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7,T8 arg8, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,T4,T5,T6,T7,T8,T9,TReturn>(Func<T1,T2,T3,T4,T5,T6,T7,T8,T9, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7,T8 arg8, T9 arg9, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9},connection);
        public Task<TReturn> CallAsync<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,TReturn>(Func<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10, NetConnection, Task<TReturn>> method,T1 arg1,T2 arg2,T3 arg3,T4 arg4,T5 arg5,T6 arg6,T7 arg7,T8 arg8, T9 arg9, T10 arg10, NetConnection connection = null) => CreateAndCallAsync<TReturn>(method.Method,new object[]{arg1,arg2,arg3,arg4,arg5,arg6,arg7,arg8,arg9,arg10},connection);

        private void CreateAndCall(MethodInfo info, object[] args = null, NetConnection recipient = null)
        {
            var netMessage = CreateMessage(info, CommunicationType.Call, args);
            if (recipient == null)
            {
                NetEncryptor.Encrypt(netMessage);
                (NetConnector as NetClient)?.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                NetEncryptor.Encrypt(netMessage,recipient);
                (NetConnector as NetServer)?.SendMessage(netMessage, recipient, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        private NetOutgoingMessage CreateMessage(MethodInfo info, CommunicationType type,object[] args = null)
        {
            var callMessage =
                Converter.CreateSendCallMessage(_sendAddressDictionary[info.Name], info.GetParameters(), args);
            var netMessage = NetConnector.CreateMessage();
            netMessage.Write((byte)type);
            netMessage.Write(callMessage.Key);
            netMessage.Write(Converter.SerializeCallMessage(callMessage));
            return netMessage;
        }
        
        private Task<TReturn> CreateAndCallAsync<TReturn>(MethodInfo info, object[] args = null,
            NetConnection recipient = null)
        {
            var netMessage = CreateMessage(info, CommunicationType.CallAsync, args);
            var taskKey = Guid.NewGuid().ToString();
            netMessage.Write(taskKey);

            if (recipient == null)
            {
                NetEncryptor.Encrypt(netMessage);
                (NetConnector as NetClient)?.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                NetEncryptor.Encrypt(netMessage,recipient);
                (NetConnector as NetServer)?.SendMessage(netMessage, recipient, NetDeliveryMethod.ReliableOrdered, 0);
            }

            return Task.Run((() =>
            {
                var time = DateTime.Now;
                _awaitingCalls.Add(taskKey, new AwaitingCallJob(){ReturnType = typeof(TReturn),Data = null});
                while (true)
                {
                    var elapsedTime = (DateTime.Now-time).TotalSeconds;
                    if (elapsedTime >= CommunicationSettings.AwaitCallTimeOut)
                    {
                        _awaitingCalls.Remove(taskKey);
                        throw new CommunicationTimeOutException($"Calling {info.Name} took longer than configured time in CommunicationSettings, elapsed time: {elapsedTime:F1}");
                    }
                    if (_awaitingCalls[taskKey].Data != null)
                        break;
                }
                var data = (TReturn) _awaitingCalls[taskKey].Data;
                _awaitingCalls.Remove(taskKey);
                return data;
            }));
        }

        private void SendAwaitedReturnMessage(string identifier, object result, NetConnection connection)
        {
            var netMessage = NetConnector.CreateMessage();
            netMessage.Write((byte)CommunicationType.CallAsyncReturn);
            netMessage.Write(identifier);
            netMessage.Write(Converter.SerializeArgument(result,result.GetType()));
            if (NetConnector is NetServer server)
            {
                server.SendMessage(netMessage, connection, NetDeliveryMethod.ReliableOrdered, 0);
            }
            else
            {
                (NetConnector as NetClient)?.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered);
            }
        }
        protected void FilterMessage(NetIncomingMessage message)
        {
            var messageType = (CommunicationType)message.ReadByte();
            if (messageType == CommunicationType.CallAsyncReturn)
            {
                var identifier = message.ReadString();
                var awaitingCall = _awaitingCalls[identifier];
                awaitingCall.Data = Converter.DeserializeArgument(message.ReadString(), awaitingCall.ReturnType);
                return;
            }
            var key = message.ReadUInt16();
            var pointer = _recieveFilters[key];
            var args = Converter.HandleRecieveMessage(message.ReadString(), pointer,message.SenderConnection);
            switch (messageType)
            {
                case CommunicationType.Call:
                    try
                    {
                        if (pointer.Method.ReturnType == typeof(Task))
                        {
                            AddRunningTask((Task)pointer.Method.Invoke(this, args));
                        }
                        else
                        {
                            pointer.Method.Invoke(this, args);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex.InnerException ?? ex);
                    }
                    break;
                case CommunicationType.CallAsync:
                    try
                    {
                        var taskKey = message.ReadString();
                        AddRunningTask((Task)pointer.Method.Invoke(this,args),pointer.Method.ReturnType,taskKey,message.SenderConnection);
                    }
                    catch (Exception ex)
                    {
                        Log(ex.InnerException ?? ex);
                    }
                    break;
                case CommunicationType.CallAsyncReturn:

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }

        protected void RunTasks()
        {
            for (var i = _runningTasks.Count; i-- > 0;)
            {
                var job = _runningTasks[i];
                var task = (Task) job.Task;
                switch (task.Status)
                {
                    case TaskStatus.Created:
                        break;
                    case TaskStatus.WaitingForActivation:
                        break;
                    case TaskStatus.WaitingToRun:
                        break;
                    case TaskStatus.Running:
                        break;
                    case TaskStatus.WaitingForChildrenToComplete:
                        break;
                    case TaskStatus.RanToCompletion:
                        _runningTasks.Remove(job);
                        var type = job.TaskType;
                        if (type == null)
                        {
                            
                        }
                        else if (type == typeof(Task))
                        {
                            
                        }
                        else
                        {
                            var asd = job.Task.GetType().GetProperty("Result").GetValue(job.Task, null);
                            SendAwaitedReturnMessage(job.Identifier,asd,job.Reciever);
                        }
                        break;
                    case TaskStatus.Canceled:
                        _runningTasks.Remove(job);
                        if(task.Exception != null)
                            ExceptionsCaught.Add(task.Exception);
                        break;
                    case TaskStatus.Faulted:
                        _runningTasks.Remove(job);
                        if (task.Exception != null)
                            ExceptionsCaught.Add(task.Exception);
                        break;
                }
            }
        }
        protected virtual void Log(object message, [CallerMemberName] string caller = null)
        {
            _onLoggedEvent?.Invoke(message.ToString(),caller);
            _logList.Add(new Tuple<object, string>(message, caller));
        }
        protected void AddRunningTask(Task task,Type taskType = null,string identifier = null,NetConnection reciever = null)
        {
            _runningTasks.Add(new TaskJob(){Task = task,TaskType = taskType,Identifier = identifier,Reciever = reciever});
        }
        public virtual void Tick(int interval)
        {
            
        }

        public virtual void CloseConnection()
        {
            NetConnector.Shutdown("shutdown");
        }

        
        protected virtual void OnConnectionStatusChanged(NetConnectionStatus status,NetConnectionResult connectionResult, NetConnection connection)
        {
            ConnectionStatus = status;
            OnConnectionStatusChangedEvent?.Invoke(status,connectionResult);
            switch (status)
            {
                case NetConnectionStatus.None:
                    break;
                case NetConnectionStatus.InitiatedConnect:
                    break;
                case NetConnectionStatus.ReceivedInitiation:
                    break;
                case NetConnectionStatus.RespondedAwaitingApproval:
                    break;
                case NetConnectionStatus.RespondedConnect:
                    break;
                case NetConnectionStatus.Connected:
                    OnConnected(connection);
                    break;
                case NetConnectionStatus.Disconnecting:
                    break;
                case NetConnectionStatus.Disconnected:
                    OnDisconnected_Internal(connection);
                    OnDisconnected(connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
            Log(status);
        }

        protected virtual void OnDisconnected_Internal(NetConnection connection)
        {
            
        }
        protected abstract void OnDisconnected(NetConnection connection);
        protected abstract void OnConnected(NetConnection connection);

        private class AwaitingCallJob
        {
            public Type ReturnType { get; set; }
            public object Data { get; set; }
        }
        private class TaskJob
        {
            public object Task { get; set; }
            public Type TaskType { get; set; }
            public string Identifier { get; set; }
            public NetConnection Reciever { get; set; }
        }
          
    }
    public class MessageFilter
    {
        public MethodInfo Method;
        public Type[] Types;
    }
    public class CallMessage
    {
        public ushort Key;
        public string[] Args;
    }
}
