﻿//
//  This file is part of Diwen.xbrl.
//
//  Author:
//       John Nordberg <john.nordberg@gmail.com>
//
//  Copyright (c) 2015-2020 John Nordberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace Diwen.Xbrl
{

    using System;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using Diwen.Xbrl.Extensions;

    [XmlRoot(ElementName = "explicitMember", Namespace = "http://xbrl.org/2006/xbrldi")]
    public struct ExplicitMember : IXmlSerializable, IEquatable<ExplicitMember>, IComparable<ExplicitMember>
    {
        internal Instance Instance { get; set; }

        [XmlIgnore]
        public XmlQualifiedName Dimension { get; set; }

        [XmlIgnore]
        public XmlQualifiedName Value { get; set; }

        public ExplicitMember(XmlQualifiedName dimension, XmlQualifiedName value)
            : this()
        {
            Dimension = dimension;
            Value = value;
        }

        public string MemberCode
        {
            get
            {
                var prefix = Value.Prefix();
                var localname = Value.LocalName();

                if (string.IsNullOrEmpty(prefix))
                    prefix = Instance?.Namespaces?.LookupPrefix(Value.Namespace) ?? "";

                return string.Join(":", prefix, localname);
            }
        }

        public override int GetHashCode()
        => Value != null ? Value.GetHashCode() : 0;

        public override bool Equals(object obj)
        => Equals((ExplicitMember)obj);

        public override string ToString()
        => $"{Dimension.LocalName()}={MemberCode}";

        public int Compare(ExplicitMember other)
        => CompareTo(other);

        #region operator overloads

        public static bool operator ==(ExplicitMember left, ExplicitMember right)
        => left.Equals(right);

        public static bool operator !=(ExplicitMember left, ExplicitMember right)
        => !left.Equals(right);

        public static bool operator >(ExplicitMember left, ExplicitMember right)
        => left.CompareTo(right) > 0;

        public static bool operator <(ExplicitMember left, ExplicitMember right)
        => right > left;

        #endregion

        #region IXmlSerializable implementation

        public XmlSchema GetSchema()
        => null;

        public void ReadXml(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            reader.MoveToContent();
            var content = reader.GetAttribute("dimension");
            var idx = content.IndexOf(':');
            var prefix = content.Substring(0, idx);
            var ns = reader.LookupNamespace(prefix);
            var name = content.Substring(idx + 1);
            Dimension = new XmlQualifiedName(name, ns);

            content = reader.ReadString().Trim();
            idx = content.IndexOf(':');
            prefix = content.Substring(0, idx);
            ns = reader.LookupNamespace(prefix);
            name = content.Substring(idx + 1);
            Value = new XmlQualifiedName(name, ns);
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
                
            var prefix = writer.LookupPrefix(Dimension.Namespace);
            var dim = $"{prefix}:{Dimension.LocalName()}";
            writer.WriteAttributeString("dimension", dim);
            writer.WriteQualifiedName(Value.Name, Value.Namespace);
        }

        #endregion

        #region IEquatable implementation

        public bool Equals(ExplicitMember other)
        => Dimension == other.Dimension && Value == other.Value;

        #endregion

        #region IComparable implementation

        public int CompareTo(ExplicitMember other)
        {
            int result;
            result = string.Compare(Dimension.Name, other.Dimension.Name, StringComparison.Ordinal);
            if (result == 0)
                result = string.Compare(Value.Name, other.Value.Name, StringComparison.Ordinal);

            return result;
        }

        #endregion
    }
}
