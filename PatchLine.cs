/* ----------------------------------------------------------------------------
Transonic Patch Library
Copyright (C) 1995-2017  George E Greaney

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml;

namespace Transonic.Patch
{
    public class PatchLine
    {
        public PatchCanvas canvas;
        public PatchPanel srcPanel;
        public Point srcEnd;
        public PatchPanel destPanel;
        public Point destEnd;
        public iPatchConnector connector;       //connector in the backing model

        public bool isSelected;

        readonly Pen CONNECTORCOLOR = new Pen(Color.Red, 2.0f);
        readonly Pen SELECTEDCOLOR = new Pen(Color.Blue, 2.0f);
        
        //for connecting panels by user
        //new line starts at source panel's output jack, the input end follows the mouse until it is dropped on a target panel
        public PatchLine(PatchCanvas _canvas, PatchPanel _srcPanel, Point _destEnd)
        {
            canvas = _canvas;
            connectSourceJack(_srcPanel);
            destPanel = null;
            destEnd = _destEnd;
            connector = null;
            isSelected = false;
        }

        //for reloading existing connections from a stored patch
        public PatchLine(PatchCanvas _canvas, PatchPanel srcPanel, PatchPanel destPanel)
        {
            canvas = _canvas;
            connectSourceJack(srcPanel);
            connectDestJack(destPanel);
            isSelected = false;
        }

//- connections ---------------------------------------------------------------

        public void connectSourceJack(PatchPanel _srcPanel)
        {
            srcPanel = _srcPanel;
            srcEnd = srcPanel.getConnectionPoint();
            srcPanel.connectLine(this);            
        }

        public void connectDestJack(PatchPanel _destPanel)
        {
            destPanel = _destPanel;
            destEnd = destPanel.getConnectionPoint();
            destPanel.connectLine(this);                            //connect line & dest panel in view            
            connector = srcPanel.makeConnection(destPanel);                     //connect panels in model
            connector.setLine(this);
        }

        public void disconnect()
        {
            if (srcPanel != null)
            {
                srcPanel.breakConnection(destPanel);                        //disconnect panels in model
                srcPanel.disconnectLine(this);
            }
            srcPanel = null;
            if (destPanel != null) destPanel.disconnectLine(this);
            destPanel = null;                                           //disconnect line from both panels in view
        }

        public void setSourceEndPos(Point _srcEnd)
        {
            srcEnd = _srcEnd;
        }

        public void setDestEndPos(Point _destEnd)
        {
            destEnd = _destEnd;
        }

//- displaying ----------------------------------------------------------------

        public bool hitTest(Point p)
        {
            //bounding box
            if (p.X < (srcEnd.X < destEnd.X ? srcEnd.X : destEnd.X) ||
                p.X > (srcEnd.X > destEnd.X ? srcEnd.X : destEnd.X) ||
                p.Y < (srcEnd.Y < destEnd.Y ? srcEnd.Y : destEnd.Y) ||
                p.Y > (srcEnd.Y > destEnd.Y ? srcEnd.Y : destEnd.Y))
                return false;

            //if inside bounding box, calc distance from point to line
            int lineX = destEnd.X - srcEnd.X;
            int lineY = destEnd.Y - srcEnd.Y;
            int pointX = srcEnd.X - p.X;
            int pointY = srcEnd.Y - p.Y;
            double lineLen = Math.Sqrt(lineX * lineX + lineY + lineY);
            double projLine = (lineX * pointY - lineY * pointX);
            double dist = Math.Abs(projLine / lineLen);
            return (dist < 2.0);
        }

        public void setSelected(bool _selected)
        {
            isSelected = _selected;
        }

//- user input ----------------------------------------------------------------

        public void onDoubleClick(Point pos)
        {
            connector.onDoubleClick(pos);
        }

        public void onRightClick(Point pos)
        {
            connector.onRightClick(pos);
        }

//- painting ------------------------------------------------------------------

        public void paint(Graphics g)
        {
            g.DrawLine(isSelected ? SELECTEDCOLOR : CONNECTORCOLOR, srcEnd, destEnd);
        }

//- persistance ---------------------------------------------------------------

        public static PatchLine loadFromXML(PatchCanvas canvas, XmlNode lineNode)
        {
            int srcBoxNum = Convert.ToInt32(lineNode.Attributes["sourcebox"].Value);
            int srcPanelNum = Convert.ToInt32(lineNode.Attributes["sourcepanel"].Value);
            int destBoxNum = Convert.ToInt32(lineNode.Attributes["destbox"].Value);
            int destPanelNum = Convert.ToInt32(lineNode.Attributes["destpanel"].Value);

            PatchLine line = null;
            PatchBox sourceBox = canvas.findPatchBox(srcBoxNum);
            PatchBox destBox = canvas.findPatchBox(destBoxNum);
            if (sourceBox != null && destBox != null)
            {
                PatchPanel sourcePanel = sourceBox.findPatchPanel(srcPanelNum);
                PatchPanel destPanel = destBox.findPatchPanel(destPanelNum);

                line = new PatchLine(canvas, sourcePanel, destPanel);
                line.connector.loadFromXML(lineNode);
            }
            return line;
        }

        public void saveToXML(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("connection");
            xmlWriter.WriteAttributeString("sourcebox", srcPanel.patchbox.boxNum.ToString());
            xmlWriter.WriteAttributeString("sourcepanel", srcPanel.panelNum.ToString());
            xmlWriter.WriteAttributeString("destbox", destPanel.patchbox.boxNum.ToString());
            xmlWriter.WriteAttributeString("destpanel", destPanel.panelNum.ToString());

            connector.saveToXML(xmlWriter);         //save model attributes

            xmlWriter.WriteEndElement();
        }
    }
}

//Console.WriteLine("there's no sun in the shadow of the Wizard");
