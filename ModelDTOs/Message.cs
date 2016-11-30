namespace ModelDTOs
{
    using System.Collections.Generic;
    using System.Resources;

    using ModelDTOs.Entities;
    using ModelDTOs.Enums;

    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(10, typeof(Message<string>))]
    [ProtoInclude(20, typeof(Message<UserFull>))]
    [ProtoInclude(50, typeof(Message<ICollection<UserLimited>>))]
    [ProtoInclude(30, typeof(Message<PlayerDTO>))]
    [ProtoInclude(40, typeof(Message<ICollection<PlayerDTO>>))]
    [ProtoInclude(60, typeof(Message<ResourceProviderDTO>))]
    [ProtoInclude(80, typeof(Message<ICollection<ResourceProviderDTO>>))]
    [ProtoInclude(70, typeof(Message<UnitDTO>))]
    [ProtoInclude(90, typeof(Message<ICollection<UnitDTO>>))]
    [ProtoInclude(100, typeof(Message<ResourceSet>))]
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