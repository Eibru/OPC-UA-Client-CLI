using Opc.Ua;
using Opc.Ua.Client;
using OpcUaCli.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpcUaCli.Client;
public class OpcController {
    public string Host { get; private set; }
    public bool Connected { get; private set; }

    private Session _session;

    private List<uint> AttributeIds = new List<uint> {
        Attributes.AccessLevel,
        Attributes.AccessLevelEx,
        Attributes.AccessRestrictions,
        Attributes.ArrayDimensions,
        Attributes.BrowseName,
        Attributes.ContainsNoLoops,
        Attributes.DataType,
        Attributes.DataTypeDefinition,
        Attributes.Description,
        Attributes.DisplayName,
        Attributes.EventNotifier,
        Attributes.Executable,
        Attributes.Historizing,
        Attributes.InverseName,
        Attributes.IsAbstract,
        Attributes.MinimumSamplingInterval,
        Attributes.NodeClass,
        Attributes.NodeId,
        Attributes.RolePermissions,
        Attributes.Symmetric,
        Attributes.UserAccessLevel,
        Attributes.UserExecutable,
        Attributes.UserRolePermissions,
        Attributes.UserWriteMask,
        Attributes.Value,
        Attributes.ValueRank,
        Attributes.WriteMask
    };

    private string GetAttributeName(uint attr) {
        switch (attr) {
            case Attributes.AccessLevel:
                return "AccessLevel";
            case Attributes.AccessLevelEx:
                return "AccessLevelEx";
            case Attributes.AccessRestrictions:
                return "AccessRestrictions";
            case Attributes.ArrayDimensions:
                return "ArrayDimensions";
            case Attributes.BrowseName:
                return "BrowseName";
            case Attributes.ContainsNoLoops:
                return "ContainsNoLoops";
            case Attributes.DataType:
                return "DataType";
            case Attributes.DataTypeDefinition:
                return "DataTypeDefinition";
            case Attributes.Description:
                return "Description";
            case Attributes.DisplayName:
                return "DisplayName";
            case Attributes.EventNotifier:
                return "EventNotifier";
            case Attributes.Executable:
                return "Executable";
            case Attributes.Historizing:
                return "Historizing";
            case Attributes.InverseName:
                return "InverseName";
            case Attributes.IsAbstract:
                return "IsAbstract";
            case Attributes.MinimumSamplingInterval:
                return "MinimumSamplingInterval";
            case Attributes.NodeClass:
                return "NodeClass";
            case Attributes.NodeId:
                return "NodeId";
            case Attributes.RolePermissions:
                return "RolePermissions";
            case Attributes.Symmetric:
                return "Symmetric";
            case Attributes.UserAccessLevel:
                return "UserAccessLevel";
            case Attributes.UserExecutable:
                return "UserExecutable";
            case Attributes.UserRolePermissions:
                return "UserRolePermissions";
            case Attributes.UserWriteMask:
                return "UserWriteMask";
            case Attributes.Value:
                return "Value";
            case Attributes.ValueRank:
                return "ValueRank";
            case Attributes.WriteMask:
                return "WriteMask";

            default:
                return "Unknown";
        }
    }

    public async Task Connect(string host) {
        var applicationName = "testApp";
        var sessionName = "testSession";

        var config = new ApplicationConfiguration();
        config.ApplicationName = applicationName;
        config.ClientConfiguration = new ClientConfiguration();
        config.SecurityConfiguration = new SecurityConfiguration() {
            ApplicationCertificate = new CertificateIdentifier(),
            TrustedIssuerCertificates = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
            TrustedPeerCertificates = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" }
        };


        var endpointConfiguration = EndpointConfiguration.Create(config);
        var endpointDescription = await CoreClientUtils.SelectEndpointAsync(config, host, false);

        var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
        var session = await Session.CreateAsync(config, null, configuredEndpoint, false, false, sessionName, 5000, null, null);

        _session = session;

        Connected = true;
        Host = _session.Endpoint.EndpointUrl;
    }

    public void Distonnect() {
        _session.Dispose();
        Connected = false;
    }

    public async Task<List<NodeAttributeDTO>> ReadNodeAttributes(string nodeId) {
        var readValueIdCollection = new ReadValueIdCollection();
        var node = await _session.ReadNodeAsync(nodeId);

        foreach (var attr in AttributeIds) {
            if (node.SupportsAttribute(attr)) {
                readValueIdCollection.Add(new ReadValueId {
                    NodeId = node.NodeId,
                    AttributeId = attr
                });
            }
        }

        var response = await _session.ReadAsync(null, 10, TimestampsToReturn.Both, readValueIdCollection, CancellationToken.None);

        var list = new List<NodeAttributeDTO>();
        for (int i = 0; i < response.Results.Count; i++) {
            var attributeId = readValueIdCollection[i].AttributeId;
            var name = GetAttributeName(attributeId);
            var value = response.Results[i].Value;

            if (attributeId == Attributes.NodeClass) {
                value = ((NodeClass)value).ToString();
            } else if (attributeId == Attributes.DataType) {
                var dataTypeNode = await _session.ReadNodeAsync((NodeId)value);

                value = dataTypeNode.DisplayName;
            }

            list.Add(new NodeAttributeDTO { Name = name, Value = value, AttributeId = attributeId });
        }
        
        return list;
    }

    public async Task<NodeDTO> ReadNode(string nodeId) {
        var node = await _session.ReadNodeAsync(nodeId);

        return new NodeDTO {
            Name = node.DisplayName.Text,
            NodeId = node.NodeId.ToString(),
            NodeClass = node.NodeClass,
            Attributes = await ReadNodeAttributes(nodeId)
        };
    }

    public async Task Write(string nodeId, uint attributeId, object value) {
        var nodeToWrite = new WriteValue {
            NodeId = nodeId,
            AttributeId = attributeId,
            Value = new DataValue(new Variant(value))
        };

        var nodesToWrite = new WriteValueCollection { nodeToWrite };

        await _session.WriteAsync(null, nodesToWrite, CancellationToken.None);
    }

    public async Task<List<NodeDTO>> Browse(string nodeId) {
        var (responseHeader, bytes, rdc) = await _session.BrowseAsync(null, null, nodeId, 0, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true, (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method | (uint)NodeClass.VariableType | (uint)NodeClass.DataType | (uint)NodeClass.ReferenceType | (uint)NodeClass.ObjectType | (uint)NodeClass.Unspecified | (uint)NodeClass.View);
        var list = new List<NodeDTO>();

        foreach(var node in rdc) {
            list.Add(new NodeDTO { Name = node.DisplayName.Text, NodeId = node.NodeId.ToString(), NodeClass = node.NodeClass, Attributes = await ReadNodeAttributes(node.NodeId.ToString()) });
        }

        return list;
    }

    private async Task BrowseAll(NodeId node, int depth = 0) {
        var (responseHeader, bytes, rdc) = await _session.BrowseAsync(null, null, node, 0, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true, (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method);

        foreach (var item in rdc) {
            string spaces = "";
            for (int i = 0; i < depth; i++) {
                spaces += "    ";
            }


            Console.WriteLine(spaces + item.DisplayName.Text + " - " + item.NodeId.ToString() + " - " + item.NodeClass.ToString());
            if (item.NodeClass == NodeClass.Variable) {
                DataValue value = null;

                try {
                    value = await _session.ReadValueAsync(((NodeId)item.NodeId));
                } catch (Exception ex) {

                }

                Console.WriteLine(spaces + "    " + value?.ToString());
            }

            await BrowseAll(((NodeId)item.NodeId), depth + 1);
        }
    }

}
