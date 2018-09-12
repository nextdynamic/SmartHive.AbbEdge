﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using abbRemoteMonitoringGateway.Services.Exceptions;
using abbRemoteMonitoringGateway.Services.Diagnostics;

namespace abbRemoteMonitoringGateway.Models
{
 
    public class DeviceModel
    {
        // Note: Storage records' payload don't contain the ETag.
        //       ETag is defined by the storage engine, e.g. with a dedicated field.
        [JsonIgnore]
        public string ETag { get; set; }

        public string Id { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DeviceModelType Type { get; set; }
        public IoTHubProtocol Protocol { get; set; }

        public Dictionary<string, object> Properties { get; set; }
        public IList<DeviceModelMessage> Telemetry { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }

        public DeviceModel()
        {
            this.ETag = string.Empty;
            this.Id = string.Empty;
            this.Version = "0.0.0";
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.Type = DeviceModelType.Custom;
            this.Protocol = IoTHubProtocol.AMQP;
            this.Properties = new Dictionary<string, object>();
            this.Telemetry = new List<DeviceModelMessage>();
            
        }

        /// <summary>
        /// This data is published in the device twin, so that clients
        /// parsing the telemetry have information about the schema used,
        /// e.g. whether the format is JSON or something else, the list of
        /// fields and their type.
        /// </summary>
        public JObject GetTelemetryReportedProperty(ILogger log)
        {
            var result = new JObject();

            foreach (var t in this.Telemetry)
            {
                if (t == null)
                {
                    log.Error("The device model contains an invalid message definition",
                        () => new { this.Id, this.Name });
                    throw new InvalidConfigurationException("The device model contains an invalid message definition");
                }

                if (string.IsNullOrEmpty(t.MessageSchema.Name))
                {
                    log.Error("One of the device messages schema doesn't have a name specified",
                        () => new { this.Id, this.Name, t });
                    throw new InvalidConfigurationException("One of the device messages schema doesn't have a name specified");
                }

                var fields = new JObject();
                foreach (var field in t.MessageSchema.Fields)
                {
                    fields[field.Key] = field.Value.ToString();
                }

                var schema = new JObject
                {
                    ["Name"] = t.MessageSchema.Name,
                    ["ClassName"] = t.MessageSchema.ClassName,
                    ["Format"] = t.MessageSchema.Format.ToString(),
                    ["Fields"] = fields
                };

                var message = new JObject
                {
                    ["Interval"] = t.Interval,
                    ["MessageTemplate"] = t.MessageTemplate,
                    ["MessageSchema"] = schema
                };

                result[t.MessageSchema.Name] = message;
            }

            return result;
        }

    

        public class DeviceModelMessage
        {
            public TimeSpan Interval { get; set; }
            public string MessageTemplate { get; set; }
            public DeviceModelMessageSchema MessageSchema { get; set; }

            public DeviceModelMessage()
            {
                this.Interval = TimeSpan.Zero;
                this.MessageTemplate = string.Empty;
                this.MessageSchema = new DeviceModelMessageSchema();
            }
        }

        public class DeviceModelMessageSchema
        {
            public string Name { get; set; }

            public string ClassName { get; set; }

            public DeviceModelMessageSchemaFormat Format { get; set; }

            public IDictionary<string, DeviceModelMessageSchemaType> Fields { get; set; }

            public DeviceModelMessageSchema()
            {
                this.Name = string.Empty;
                this.ClassName = string.Empty;
                this.Format = DeviceModelMessageSchemaFormat.JSON;
                this.Fields = new Dictionary<string, DeviceModelMessageSchemaType>();
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DeviceModelMessageSchemaFormat
        {
            [EnumMember(Value = "Binary")]
            Binary = 0,

            [EnumMember(Value = "Text")]
            Text = 10,

            [EnumMember(Value = "JSON")]
            JSON = 20,

            [EnumMember(Value = "Protobuf")]
            Protobuf = 30
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DeviceModelMessageSchemaType
        {
            [EnumMember(Value = "Object")]
            Object = 0,

            [EnumMember(Value = "Binary")]
            Binary = 10,

            [EnumMember(Value = "Text")]
            Text = 20,

            [EnumMember(Value = "Boolean")]
            Boolean = 30,

            [EnumMember(Value = "Integer")]
            Integer = 40,

            [EnumMember(Value = "Double")]
            Double = 50,

            [EnumMember(Value = "DateTime")]
            DateTime = 60
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DeviceModelType
        {
            [EnumMember(Value = "Undefined")]
            Undefined = 0,

            [EnumMember(Value = "Stock")]
            Stock = 10,

            [EnumMember(Value = "Custom")]
            Custom = 20
        }
    }
}