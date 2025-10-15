using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaCli.Client.Models;
public class NodeAttributeDTO {
    public string Name { get; set; }
    public uint AttributeId { get; set; }
    public object Value { get; set; }
}
