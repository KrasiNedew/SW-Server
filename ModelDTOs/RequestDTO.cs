namespace ModelDTOs
{
    using System;
    using ModelDTOs.Enums;

    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(10, typeof(RequestDTO<string>))]
    [ProtoInclude(20, typeof(RequestDTO<AuthDataRawDTO>))]
    [ProtoInclude(30, typeof(RequestDTO<PlayerDTO>))]
    public abstract class RequestDTO
    {
        protected RequestDTO()
        {
        }

        protected RequestDTO(ServiceRequest request, Type dataType)
        {
            this.Request = request;
            this.DataType = dataType;
        }

        [ProtoMember(1)]
        public ServiceRequest Request { get; set; }

        [ProtoMember(2)]
        public Type DataType { get; set; }
    }

    [ProtoContract]
    public class RequestDTO<T> : RequestDTO
    {
        protected RequestDTO()
        {
        }

        public RequestDTO(ServiceRequest request, Type dataType, T data)
            : base(request, dataType)
        {
            this.Data = data;
        }

        [ProtoMember(3)]
        public T Data { get; set; }
    }
}