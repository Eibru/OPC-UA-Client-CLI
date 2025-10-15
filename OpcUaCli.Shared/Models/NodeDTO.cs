using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaCli.Shared.Models;
public class NodeDTO {
    public string NodeId { get; set; }
    public string Name { get; set; }
    public List<NodeAttributeDTO> Attributes { get; set; }

    public override string ToString() => Name;
}
