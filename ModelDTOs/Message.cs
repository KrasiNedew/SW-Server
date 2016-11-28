namespace ModelDTOs
{
    using ModelDTOs.Enums;

    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(10, typeof(Message<string>))]
    [ProtoInclude(20, typeof(Message<AuthDataRaw>))]
    [ProtoInclude(30, typeof(Message<PlayerDTO>))]
    [ProtoInclude(40, typeof(Message<PlayerDTO[]>))]
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