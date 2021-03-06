﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorFly.Grpc.Internal;
using BlazorFly.Grpc.Tests.Core;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.AspNetCore.Components.Testing;
using Moq;
using Xunit;

namespace BlazorFly.Grpc.Tests.UnitTests
{
    public class ComponentTests
    {
        private readonly TestHost host = new TestHost();

        public ComponentTests()
        {
            host.AddService<IGrpcViewTypeProvider>(new GrpcViewTypeProvider(new List<Type> { typeof(TestService.TestServiceClient) }));
            host.AddService<IGrpcMetadataProvider>(new GrpcMetadataProvider(new Metadata()));
        }

        [Fact]
        public void ExecuteUnaryCall_WhenItMapped_ShouldUpdateResponseMarkup()
        {
            // Arrange
            const string ExpectedMessage = "Unusual Markup Message Unary Call";            

            var clientMock = new Mock<TestService.TestServiceClient>();
            clientMock.Setup(m => m.UnaryCallAsync(It.IsAny<HelloRequest>(), It.IsAny<CallOptions>()))
                .Returns(TestCalls.AsyncUnaryCall(
                Task.FromResult(new HelloResponse { Message = ExpectedMessage }),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

            host.AddService(clientMock.Object);
            var component = host.AddComponent<TestWrapper>();

            // Act
            component.Find("button.execute.unaryCall").Click();
            host.WaitForNextRender();
            var markup = component.GetMarkup();

            // Assert
            Assert.Contains(ExpectedMessage, markup);
        }

        [Fact]
        public void ExecuteClientStreaming_WhenItMapped_ShouldUpdateResponseMarkup()
        {
            // Arrange
            const string ExpectedMessage = "Unusual Markup Message Client Streaming";

            var clientMock = new Mock<TestService.TestServiceClient>();
            clientMock.Setup(m => m.ClientStreaming(It.IsAny<CallOptions>()))
                .Returns(TestCalls.AsyncClientStreamingCall(
                    new AsyncStreamWriterStub(),
                    Task.FromResult(new HelloResponse { Message = ExpectedMessage }),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { }));

            host.AddService(clientMock.Object);
            var component = host.AddComponent<TestWrapper>();

            // Act
            component.Find("button.execute.clientStreaming").Click();
            host.WaitForNextRender();
            var markup = component.GetMarkup();

            // Assert
            Assert.Contains(ExpectedMessage, markup);
        }

        [Fact]
        public void ExecuteServerStreaming_WhenItMapped_ShouldUpdateResponseMarkup()
        {
            // Arrange
            const string ExpectedMessage = "Unusual Markup Message Server Streaming";

            var clientMock = new Mock<TestService.TestServiceClient>();
            clientMock.Setup(m => m.ServerStreaming(It.IsAny<HelloRequest>(), It.IsAny<CallOptions>()))
                .Returns(TestCalls.AsyncServerStreamingCall(
                    new AsyncStreamReaderStub(ExpectedMessage),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { }));

            host.AddService(clientMock.Object);
            var component = host.AddComponent<TestWrapper>();

            // Act
            component.Find("button.execute.serverStreaming").Click();
            host.WaitForNextRender();
            var markup = component.GetMarkup();

            // Assert
            Assert.Contains(ExpectedMessage, markup);
        }

        [Fact]
        public void ExecuteDuplexStreaming_WhenItMapped_ShouldUpdateResponseMarkup()
        {
            // Arrange
            const string ExpectedMessage = "Unusual Markup Message Duplex Streaming";

            var clientMock = new Mock<TestService.TestServiceClient>();
            clientMock.Setup(m => m.DuplexStreaming(It.IsAny<CallOptions>()))
                .Returns(TestCalls.AsyncDuplexStreamingCall(
                    new AsyncStreamWriterStub(),
                    new AsyncStreamReaderStub(ExpectedMessage),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { }));

            host.AddService(clientMock.Object);
            var component = host.AddComponent<TestWrapper>();

            // Act
            component.Find("button.execute.duplexStreaming").Click();
            host.WaitForNextRender();
            var markup = component.GetMarkup();

            // Assert
            Assert.Contains(ExpectedMessage, markup);
        }
    }
}