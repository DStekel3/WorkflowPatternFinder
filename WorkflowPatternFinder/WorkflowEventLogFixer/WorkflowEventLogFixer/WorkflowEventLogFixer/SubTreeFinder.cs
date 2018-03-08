using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogFixer
{
    public static class SubTreeFinder
    {
        public static bool IsValidSubTree(ProcessTree tree, ProcessTree pattern, bool induced)
        {
            var tNode = tree.GetRoot();
            var pNode = pattern.GetRoot();
            return DoesBranchContainPattern(tNode, pNode, induced);
        }

        private static bool DoesBranchContainPattern(Node tNode, Node pNode, bool induced)
        {
            // if current node in tree = current node in pattern
            if(tNode.GetEvent() == pNode.GetEvent())
            {
                var pChildren = pNode.GetChildren();
                var tChildren = tNode.GetChildren();

                // check the siblings-condition. All siblings in the pattern should be included in the tree.
                if(!ContainsSiblings(tNode, pNode))
                {
                    return false;
                }

                // continue searching for children of the current pattern node.
                if(pChildren.Any())
                {
                    foreach(var patternChild in pChildren)
                    {
                        // For every tree child, check whether this child can be mapped to a pattern child.
                        foreach(Node treeChild in tChildren)
                        {
                            var patternChildFound = DoesBranchContainPattern(treeChild, patternChild, induced);
                            if(patternChildFound)
                            {
                                return true;
                            }
                        }
                        break;
                    }
                }

                // in case the current pattern node is not a parent, then we can say that the subtree of pNode as root occurs in the given tree.
                else
                {
                    return true;
                }
            }

            // return false if the current pattern searched is already partly found, but this node does not correspond to the pattern's structure.
            else if(induced && !pNode.IsRoot())
            {
                return false;
            }

            // continue searching for the pattern node. 
            // in the case of induced subtrees, this can only be done when the pNode is the root.
            // in the case of embedded subtrees, also internal nodes may be searched.
            if(induced && pNode.IsRoot() || !induced)
            {
                foreach(Node treeChild in tNode.GetChildren())
                {
                    var found = DoesBranchContainPattern(treeChild, pNode, induced);
                    if(found)
                    {
                        return true;
                    }
                }
            }

            // return false when none of the above searches have succeeded.
            return false;
        }

        private static bool ContainsSiblings(Node tNode, Node pNode)
        {
            if(tNode.GetSiblings().Intersect(pNode.GetSiblings()).ToList().Count == pNode.GetSiblings().Count)
            {
                return true;
            }
            return false;
        }
    }
}