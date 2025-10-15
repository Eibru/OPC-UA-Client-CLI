using Opc.Ua;
using Opc.Ua.Client;
using OpcUaCli.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpcUaCli.Client;
public class OpcBrowser {
    private OpcController _opcController;

    public NodeDTO SelectedNode { get; set; }
    public List<NodeDTO> AvailableNodes { get; set; }
    public List<NodeDTO> BrowsePath { get; set; }

    public OpcBrowser(OpcController controller) {
        _opcController = controller;
        AvailableNodes = new List<NodeDTO>();
        BrowsePath = new List<NodeDTO>();
    }

    public async Task<bool> Init() {
        var node = await _opcController.ReadNode(ObjectIds.RootFolder.ToString());
        var nodes = await _opcController.Browse(node.NodeId);
        BrowsePath.Add(node);
        SelectedNode = node;

        AvailableNodes = nodes;
        
        return true;
    }

    public async Task<bool> NavigateTo(string name) {
        var node = AvailableNodes.FirstOrDefault(x => x.Name == name);
        if (node == null)
            return false;

        var nodes = await _opcController.Browse(node.NodeId);
        BrowsePath.Add(node);
        SelectedNode = node;
        AvailableNodes = nodes;

        return true;
    }

    public async Task<bool> NavigateBack() {
        if (BrowsePath.Count < 2)
            return false;
        
        var node = BrowsePath.ElementAt(BrowsePath.Count - 2);
        var nodes = await _opcController.Browse(node.NodeId);
        BrowsePath.RemoveAt(BrowsePath.Count - 1);
        SelectedNode = node;
        AvailableNodes = nodes;

        return true;
    }

    public async Task Update() {
        SelectedNode = await _opcController.ReadNode(SelectedNode.NodeId);
        AvailableNodes = await _opcController.Browse(SelectedNode.NodeId);
    }
}

public class Attribute {
    public string Name { get; set; }
    public object Value { get; set; }
}