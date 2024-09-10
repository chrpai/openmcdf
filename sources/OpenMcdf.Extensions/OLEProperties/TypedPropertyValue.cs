﻿using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal abstract class TypedPropertyValue<T> : ITypedPropertyValue
    {
        private readonly VTPropertyType _VTType;

        public PropertyType PropertyType => PropertyType.TypedPropertyValue;

        public VTPropertyType VTType => _VTType;

        protected object propertyValue = null;

        public TypedPropertyValue(VTPropertyType vtType, bool isVariant = false)
        {
            this._VTType = vtType;
            PropertyDimensions = CheckPropertyDimensions(vtType);
            this.IsVariant = isVariant;
        }

        public PropertyDimensions PropertyDimensions { get; } = PropertyDimensions.IsScalar;

        public bool IsVariant { get; } = false;

        protected virtual bool NeedsPadding { get; set; } = true;

        private PropertyDimensions CheckPropertyDimensions(VTPropertyType vtType)
        {
            if ((((ushort)vtType) & 0x1000) != 0)
                return PropertyDimensions.IsVector;
            else if ((((ushort)vtType) & 0x2000) != 0)
                return PropertyDimensions.IsArray;
            else
                return PropertyDimensions.IsScalar;
        }

        public virtual object Value
        {
            get
            {
                return propertyValue;
            }

            set
            {
                propertyValue = value;
            }
        }

        public abstract T ReadScalarValue(System.IO.BinaryReader br);

        public void Read(System.IO.BinaryReader br)
        {
            long currentPos = br.BaseStream.Position;

            switch (this.PropertyDimensions)
            {
                case PropertyDimensions.IsScalar:
                    {
                        this.propertyValue = ReadScalarValue(br);
                        int size = (int)(br.BaseStream.Position - currentPos);

                        int m = size % 4;

                        if (m > 0 && this.NeedsPadding)
                            br.ReadBytes(4 - m); // padding
                    }

                    break;

                case PropertyDimensions.IsVector:
                    {
                        uint nItems = br.ReadUInt32();

                        List<T> res = new List<T>();

                        for (int i = 0; i < nItems; i++)
                        {
                            T s = ReadScalarValue(br);

                            res.Add(s);

                            // The padding in a vector can be per-item
                            int itemSize = (int)(br.BaseStream.Position - currentPos);

                            int pad = itemSize % 4;
                            if (pad > 0 && this.NeedsPadding)
                                br.ReadBytes(4 - pad); // padding
                        }

                        this.propertyValue = res;
                        int size = (int)(br.BaseStream.Position - currentPos);

                        int m = size % 4;
                        if (m > 0 && this.NeedsPadding)
                            br.ReadBytes(4 - m); // padding
                    }

                    break;
                default:
                    break;
            }
        }

        public abstract void WriteScalarValue(System.IO.BinaryWriter bw, T pValue);

        public void Write(BinaryWriter bw)
        {
            long currentPos = bw.BaseStream.Position;
            int size;
            int m;
            switch (this.PropertyDimensions)
            {
                case PropertyDimensions.IsScalar:

                    bw.Write((ushort)_VTType);
                    bw.Write((ushort)0);

                    WriteScalarValue(bw, (T)this.propertyValue);
                    size = (int)(bw.BaseStream.Position - currentPos);
                    m = size % 4;

                    if (m > 0 && this.NeedsPadding)
                    {
                        for (int i = 0; i < 4 - m; i++) // padding
                            bw.Write((byte)0);
                    }

                    break;

                case PropertyDimensions.IsVector:

                    bw.Write((ushort)_VTType);
                    bw.Write((ushort)0);
                    bw.Write((uint)((List<T>)this.propertyValue).Count);

                    for (int i = 0; i < ((List<T>)this.propertyValue).Count; i++)
                    {
                        WriteScalarValue(bw, ((List<T>)this.propertyValue)[i]);

                        size = (int)(bw.BaseStream.Position - currentPos);
                        m = size % 4;

                        if (m > 0 && this.NeedsPadding)
                        {
                            for (int q = 0; q < 4 - m; q++) // padding
                                bw.Write((byte)0);
                        }
                    }

                    size = (int)(bw.BaseStream.Position - currentPos);
                    m = size % 4;

                    if (m > 0 && this.NeedsPadding)
                    {
                        for (int i = 0; i < 4 - m; i++) // padding
                            bw.Write((byte)0);
                    }

                    break;
            }
        }
    }
}
