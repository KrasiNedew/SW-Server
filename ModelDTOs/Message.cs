﻿namespace ModelDTOs
{
    using System.Collections.Generic;
    using ModelDTOs.Entities;
    using ModelDTOs.Enums;
    using ModelDTOs.Resources;

    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(7, typeof(Message<byte>))]
    [ProtoInclude(10, typeof(Message<string>))]
    [ProtoInclude(20, typeof(Message<UserFull>))]
    [ProtoInclude(50, typeof(Message<ICollection<UserLimited>>))]
    [ProtoInclude(30, typeof(Message<PlayerDTO>))]
    [ProtoInclude(40, typeof(Message<ICollection<PlayerDTO>>))]
    [ProtoInclude(60, typeof(Message<ResourceProviderDTO>))]
    [ProtoInclude(80, typeof(Message<ICollection<ResourceProviderDTO>>))]
    [ProtoInclude(70, typeof(Message<UnitDTO>))]
    [ProtoInclude(90, typeof(Message<ICollection<UnitDTO>>))]
    [ProtoInclude(100, typeof(Message<ResourceSetDTO>))]
    public abstract class Message
    {
        protected Message()
        {
        }

        protected Message(Service service)
        {
            this.Service = service;
        }

        [ProtoMember(1)]
        public Service Service { get; set; }

        public static Message<T> Create<T>(Service service, T data)
        {
            return new Message<T>(service, data);
        }

        public static Message<T> Create<T>(T data, Service service = Service.Info)
        {
            return new Message<T>(service, data);
        }
    }

    [ProtoContract]
    public class Message<T> : Message
    {
        protected Message()
        {
        }

        public Message(Service service, T data)
            : base(service)
        {
            this.Data = data;
        }

        [ProtoMember(3)]
        public T Data { get; set; }
    }
}