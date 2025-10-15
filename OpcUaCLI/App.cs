using Opc.Ua;
using Opc.Ua.Client;
using OpcUaCli.OPCUA;
using OpcUaCli.Shared.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaCli.Client;
public class App {
    private OpcController _controller;
    private OpcBrowser _browser;

    public App() {
        _controller = new OpcController();
    }

    public async Task Start(string host = null) {
        if(host != null) {
            try {
                await _controller.Connect(host);
                _browser = new OpcBrowser(_controller);
                await _browser.Init();
            } catch (Exception ex) { 
                Console.WriteLine("Error while connecting to host");
                return;
            }
        }


        var stop = false;

        while (!stop) {
            if(_controller.Connected && _browser != null)
                Console.Write(_controller.Host + string.Join("/", _browser.BrowsePath) + "> ");
            else
                Console.Write("> ");

            var line = Console.ReadLine()?.Split(" ", 2);
            var cmd = line[0];
            var arguments = line.Length > 1 ? line[1] : null;

            if (string.IsNullOrEmpty(cmd))
                continue;

            if (cmd == "help") {
                ShowHelp();
                continue;
            }

            if(cmd == "exit") {
                if (_controller.Connected)
                    _controller.Distonnect();

                stop = true;
                continue;
            }

            if (!_controller.Connected)
                await HandleDisconnectedState(cmd, arguments);
            else
                await HandleConnectedState(cmd, arguments);
        }
    }

    async Task HandleDisconnectedState(string cmd, string args) {
        if (cmd == "connect") {
            try {
                await _controller.Connect(args);
                _browser = new OpcBrowser(_controller);
                await _browser.Init();

            } catch (Exception ex) {
                Console.WriteLine("Error while connecting to host");
                Console.WriteLine();
            }

            return;
        }

        Console.WriteLine("Unknown command");
        Console.WriteLine();
    }

    async Task HandleConnectedState(string cmd, string args) {
        if (cmd == "ls") {
            await _browser.Update();
            ShowNodeList(_browser.AvailableNodes);
            return;
        }

        if (cmd == "cd" && args == "..") {
            await _browser.NavigateBack();
            return;
        }

        if (cmd == "cd") {
            if (string.IsNullOrEmpty(args)) {
                Console.WriteLine("Missing parameters");
                Console.WriteLine();
                return;
            }

            await _browser.NavigateTo(args);

            return;
        }

        if (cmd == "read") {
            NodeDTO node;

            if (!string.IsNullOrEmpty(args))
                node = _browser.AvailableNodes.FirstOrDefault(x => x.Name == args);
            else
                node = _browser.SelectedNode;

            if (node == null) {
                Console.WriteLine("Node was not found");
                Console.WriteLine();
                return;
            }

            ShowNodeInfo(node);

            return;
        }

        if(cmd == "write") {
            Console.WriteLine("Not implemented");
            Console.WriteLine();
            return;
        }

        if (cmd == "disconnect") {
            _controller.Distonnect();
            return;
        }


        Console.WriteLine("Unknown command");
        Console.WriteLine();
    }

    private void ShowHelp() {
        if (!_controller.Connected) {
            Console.WriteLine("Connect {HOST} - Connect to host");
            Console.WriteLine("Exit - Exit the application");
        } else {
            Console.WriteLine("{0,-15} {1}", "ls", "List directory");
            Console.WriteLine("{0,-15} {1}", "cd {Node}", "Navigate to node");
            Console.WriteLine("{0,-15} {1}", "cd ..", "Navigate to parent");
            Console.WriteLine("{0,-15} {1}", "Read", "Read attributes of current node");
            Console.WriteLine("{0,-15} {1}", "Read {Node}", "Read attributes of given node");
        } 

        Console.WriteLine();
    }

    private void ShowNodeList(List<NodeDTO> nodes) {
        if(nodes.Count == 0) {
            Console.WriteLine();
            return;
        }

        var col1Width = nodes.Max(x => x.Name.Length) + 5;
        var col2Width = nodes.Max(x => x.NodeId.Length) + 5;
        var col3Width = nodes.Max(x => x.Attributes.FirstOrDefault(x => x.AttributeId == 2)?.ToString()?.Length ?? 0) + 5;

        foreach (var item in nodes) {
            Console.WriteLine($"{{0,{-col1Width}}} {{1,{-col2Width}}} {{2,{-col3Width}}}", item.Name, item.NodeId, item.Attributes.FirstOrDefault(x => x.AttributeId == 2)?.Value?.ToString() ?? "Unknown");
        }

        Console.WriteLine();
    }

    private void ShowNodeInfo(NodeDTO node) {
        var col1Width = node.Attributes.Max(x => x.Name?.Length ?? 0) + 5;
        var col2Width = node.Attributes.Max(x => ObjectToString(x.Value)?.Length ?? 0) + 5;

        if (col2Width > 50)
            col2Width = 50;

        foreach (var item in node.Attributes) {
            Console.WriteLine($"{{0,{-col1Width}}} {{1,{-col2Width}}}", item.Name, ObjectToString(item.Value));
        }

        Console.WriteLine();
    }

    private string ObjectToString(object obj) {
        if (obj?.GetType()?.IsArray == true) {
            List<string> str = new List<string>();
            var arr = obj as IEnumerable;
            foreach (var o in arr) {
                str.Add(ObjectToString(o));
            }
            return "[" + string.Join(",", str) + "]";
        }

        if(obj is ExtensionObject extObj) {
            return ObjectToString(extObj.Body);
        }

        //if (obj is byte) {
        //    return Convert.ToString((byte)obj, 2).PadLeft(8, '0');
        //}

        return obj?.ToString();
    }
}