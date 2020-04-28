﻿using ILGPU.Runtime;
using System;
using Xunit;
using Xunit.Abstractions;

namespace ILGPU.Tests
{
    public abstract class SpecializedKernels : TestBase
    {
        protected SpecializedKernels(
            ITestOutputHelper output,
            ContextProvider contextProvider)
            : base(output, contextProvider)
        { }

        public static TheoryData<object> SpecializedValueTestData =>
            new TheoryData<object>
        {
            { default(sbyte) },
            { default(byte) },
            { default(short) },
            { default(ushort) },
            { default(int) },
            { default(uint) },
            { default(long) },
            { default(ulong) },
            { default(float) },
            { default(double) },
            { default(EmptyStruct) },
            { default(TestStruct) },
            { default(TestStructEquatable<TestStructEquatable<byte>>) },
            { default(
                TestStructEquatable<int, TestStructEquatable<short, EmptyStruct>>) },
        };

        internal static void SpecializedImplicitValueKernel<T>(
            Index1 _,
            ArrayView<T> data,
            SpecializedValue<T> value)
            where T : unmanaged, IEquatable<T>
        {
            data[0] = value;
        }

        [Theory]
        [MemberData(nameof(SpecializedValueTestData))]
        [KernelMethod(nameof(SpecializedImplicitValueKernel))]
        public void SpecializedImplicitKernel<T>(T value)
            where T : unmanaged, IEquatable<T>
        {
            var method = KernelMethodAttribute.GetKernelMethod(
                new Type[] { typeof(T) });
            var kernel = Accelerator.LoadAutoGroupedKernel(
                new Action<Index1, ArrayView<T>, SpecializedValue<T>>(
                    SpecializedImplicitValueKernel));
            using var buffer = Accelerator.Allocate<T>(1);
            kernel(
                Accelerator.DefaultStream,
                1,
                buffer.View,
                new SpecializedValue<T>(value));
            Accelerator.Synchronize();

            var expected = new T[] { value };
            Verify(buffer, expected);
        }

        internal static void SpecializedExplicitValueKernel<T>(
            ArrayView<T> data,
            SpecializedValue<T> value)
            where T : unmanaged, IEquatable<T>
        {
            data[0] = value;
        }

        [Theory]
        [MemberData(nameof(SpecializedValueTestData))]
        [KernelMethod(nameof(SpecializedExplicitValueKernel))]
        public void SpecializedExplicitKernel<T>(T value)
            where T : unmanaged, IEquatable<T>
        {
            var method = KernelMethodAttribute.GetKernelMethod(
                new Type[] { typeof(T) });
            var kernel = Accelerator.LoadKernel(
                new Action<ArrayView<T>, SpecializedValue<T>>(
                    SpecializedExplicitValueKernel));
            using var buffer = Accelerator.Allocate<T>(1);
            kernel(
                Accelerator.DefaultStream,
                (1, 1),
                buffer.View,
                new SpecializedValue<T>(value));
            Accelerator.Synchronize();

            var expected = new T[] { value };
            Verify(buffer, expected);
        }
    }
}
