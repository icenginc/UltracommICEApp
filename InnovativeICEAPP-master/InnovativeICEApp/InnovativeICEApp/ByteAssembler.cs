/**
 * Copyright (c) 2015 Ultra Communications Inc.  All rights reserved.
 * 
 * $Id: ByteAssembler.cs 369 2015-04-01 23:07:01Z vahid $
 **/

using System;
using System.Runtime.InteropServices;

namespace InnovativeICEApp
{
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    struct ByteAssembler
    {
        public enum Order { BE, LE };
        [FieldOffset(0)]
        public byte bval0;
        [FieldOffset(1)]
        public byte bval1;
        [FieldOffset(2)]
        public byte bval2;
        [FieldOffset(3)]
        public byte bval3;
        [FieldOffset(0)]
        public float fval;
        [FieldOffset(0)]
        public uint u32;
        [FieldOffset(0)]
        public ushort u16;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="init"></param>
        /// <param name="order"></param>
        /// <param name="offset"></param>
        public ByteAssembler(byte[] init, Order order, int offset)
        {
            // stupid compiler can't figure out we've got a union so *all* fields must be assigned...
            bval0 = 0;
            bval1 = 0;
            bval2 = 0;
            bval3 = 0;
            fval = 0f;
            u16 = 0;
            u32 = 0;

            switch (init.Length - offset)
            {
                case 2:
                    switch (order)
                    {
                        case Order.LE:
                            bval0 = init[0 + offset];
                            bval1 = init[1 + offset];
                            break;
                        case Order.BE:
                            bval0 = init[1 + offset];
                            bval1 = init[0 + offset];
                            break;
                    }
                    break;
                case 4:
                    switch (order)
                    {
                        case Order.LE:
                            bval0 = init[0 + offset];
                            bval1 = init[1 + offset];
                            bval2 = init[2 + offset];
                            bval3 = init[3 + offset];
                            break;
                        case Order.BE:
                            bval0 = init[3 + offset];
                            bval1 = init[2 + offset];
                            bval2 = init[1 + offset];
                            bval3 = init[0 + offset];
                            break;
                    }
                    break;
                default:
                    throw new Exception("Invalid use of ByteAssembler");
            }
        }
    }
}
