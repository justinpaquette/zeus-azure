using Zeus.Azure.Storage;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Azure.Tests.Storage
{
    public class StorageQueueProcessingServiceTests
    {
        private const long _visibilityTimeoutInSeconds = 2;
        private const long _idleWaitTimeInMilliseconds = 200;
        private const long _timingAccuracyBufferInMilliseconds = 50;

        private class StorageQueueProcessingServiceTestContext
        {
            public StorageQueueProcessingService<TestMessage> SUT { get; set; }
            public Mock<ILoggingService> MockLoggingService { get; set; }
            public Mock<IStorageQueue<TestMessage>> MockStorageQueue { get; set; }

            public CancellationTokenSource CancellationTokenSource { get; set; }

        }

        private StorageQueueProcessingServiceTestContext GetTestContext()
        {
            var configuration = new StorageQueueProcessingServiceConfiguration()
            {
                IdleWaitTimeInMilliseconds = _idleWaitTimeInMilliseconds,
                VisibilityTimeoutInSeconds = _visibilityTimeoutInSeconds
            };

            var context = new StorageQueueProcessingServiceTestContext()
            {
                MockStorageQueue = new Mock<IStorageQueue<TestMessage>>(),
                MockLoggingService = new Mock<ILoggingService>(),
                CancellationTokenSource = new CancellationTokenSource()
            };

            context.SUT = new StorageQueueProcessingService<TestMessage>(
                configuration,
                context.MockLoggingService.Object,
                context.MockStorageQueue.Object
            );

            return context;
        }

        [Test]
        public void CallProvidedTaskAsMessagesAreDequeued()
        {
            //Arrange
            var context = GetTestContext();

            var processedMessages = new List<TestMessage>();

            Func<TestMessage, Task> onMessageTask = message => Task.Run(() =>
            {
                processedMessages.Add(message);
            });

            var testMessages = Enumerable.Range(0, 10)
                .Select(i => new TestMessage(i))
                .ToArray();

            SetupMockStorageQueue(context, testMessages);

            //Act
            context.CancellationTokenSource.CancelAfter(500);
            context.SUT.ProcessQueue(onMessageTask, context.CancellationTokenSource.Token).GetAwaiter().GetResult();

            //Assert
            var result = processedMessages.Select(m => m.MessageContent);
            var expected = testMessages.Select(m => m.MessageContent);

            Assert.IsTrue(
                result.SequenceEqual(expected)
            );
        }

        [Test]
        public void DoNotReturnNullWhenQueueIsEmpty()
        {
            //Arrange
            var context = GetTestContext();

            var processedMessages = new List<TestMessage>();

            Func<TestMessage, Task> onMessageTask = message => Task.Run(() =>
            {
                processedMessages.Add(message);
            });

            var testMessages = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    if (i == 1 || i == 3)
                    {
                        return null;
                    }

                    return new TestMessage(i);
                })
                .ToArray();

            SetupMockStorageQueue(context, testMessages);

            //Act
            context.CancellationTokenSource.CancelAfter(500);
            context.SUT.ProcessQueue(onMessageTask, context.CancellationTokenSource.Token).GetAwaiter().GetResult();

            //Assert
            Assert.IsTrue(
                processedMessages.Count() == 8
            );

            Assert.IsTrue(
                !processedMessages.Any(m => m == null)
            );
        }

        [Test]
        public void WaitToDequeueWhenQueueIsEmpty()
        {
            //Arrange
            var context = GetTestContext();

            var stopWatch = new Stopwatch();
            var lastMessageElapsedMilliseconds = 0L;

            var messageElapsedTimes = new List<long>();

            Func<TestMessage, Task> onMessageTask = message => Task.Run(() =>
            {
                var messageElapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                messageElapsedTimes.Add(messageElapsedMilliseconds - lastMessageElapsedMilliseconds);

                lastMessageElapsedMilliseconds = messageElapsedMilliseconds;
            });

            var testMessages = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    if (i == 1 || i == 4)
                    {
                        return null;
                    }

                    return new TestMessage(i);
                })
                .ToArray();

            SetupMockStorageQueue(context, testMessages);

            //Act
            stopWatch.Start();

            context.CancellationTokenSource.CancelAfter(1000);
            context.SUT.ProcessQueue(onMessageTask, context.CancellationTokenSource.Token).GetAwaiter().GetResult();

            //Assert
            var maxWait = _idleWaitTimeInMilliseconds + _timingAccuracyBufferInMilliseconds;

            Assert.IsTrue(
                messageElapsedTimes[1] > _idleWaitTimeInMilliseconds &&
                messageElapsedTimes[1] <= maxWait
            );

            Assert.IsTrue(
                messageElapsedTimes[3] > _idleWaitTimeInMilliseconds &&
                messageElapsedTimes[3] <= maxWait
            );
        }

        [Test]
        public void GetMessageWithCorrectVisibilityTimeout()
        {
            //Arrange
            var context = GetTestContext();

            var processedMessages = new List<TestMessage>();

            Func<TestMessage, Task> onMessageTask = message => Task.Run(() =>
            {
                processedMessages.Add(message);
            });

            var testMessages = Enumerable.Range(0, 10)
                .Select(i => new TestMessage(i))
                .ToArray();

            SetupMockStorageQueue(context, testMessages);

            //Act
            context.CancellationTokenSource.CancelAfter(500);
            context.SUT.ProcessQueue(onMessageTask, context.CancellationTokenSource.Token).GetAwaiter().GetResult();

            //Assert
            var result = processedMessages.Select(m => m.MessageContent);
            var expected = testMessages.Select(m => m.MessageContent);

            context.MockStorageQueue.Verify(m => m.GetMessage(
                    It.Is<TimeSpan>(t => t.TotalSeconds == _visibilityTimeoutInSeconds)
                ),
                Times.AtLeast(10)
            );
        }

        [Test]
        public void DeleteMessageAfterSuccessfulProcessing()
        {
            //Arrange
            var context = GetTestContext();

            var processedMessages = new List<TestMessage>();

            Func<TestMessage, Task> onMessageTask = message => Task.Run(() =>
            {
                processedMessages.Add(message);
            });

            var testMessages = Enumerable.Range(0, 10)
                .Select(i => new TestMessage(i))
                .ToArray();

            SetupMockStorageQueue(context, testMessages);

            //Act
            context.CancellationTokenSource.CancelAfter(500);
            context.SUT.ProcessQueue(onMessageTask, context.CancellationTokenSource.Token).GetAwaiter().GetResult();

            //Assert
            foreach (var testMessage in testMessages)
            {
                context.MockStorageQueue.Verify(m => m.DeleteMessage(
                    It.Is<IStorageQueueMessage<TestMessage>>(s => s.Message == testMessage)
                ));
            }
        }

        [Test]
        public void DoNotDeleteMessageOnProcessingError()
        {
            //Arrange
            var context = GetTestContext();

            var processedMessages = new List<TestMessage>();

            Func<TestMessage, Task> onMessageTask = message => Task.Run(() =>
            {
                if (message.MessageContent == 1)
                {
                    throw new Exception("Test exception");
                }

                processedMessages.Add(message);
            });

            var testMessages = Enumerable.Range(0, 10)
                .Select(i => new TestMessage(i))
                .ToArray();

            SetupMockStorageQueue(context, testMessages);

            //Act
            context.CancellationTokenSource.CancelAfter(500);
            context.SUT.ProcessQueue(onMessageTask, context.CancellationTokenSource.Token).GetAwaiter().GetResult();

            //Assert
            context.MockStorageQueue.Verify(m => m.DeleteMessage(
                    It.Is<IStorageQueueMessage<TestMessage>>(s => s.Message.MessageContent == 1)
                ),
                Times.Never
            );
        }

        [Test]
        public void RenewMessageWhileProcessingIsOngoing()
        {
            //Arrange
            var context = GetTestContext();

            var processedMessages = new List<TestMessage>();

            Func<TestMessage, Task> onMessageTask = message => Task.Run(async () =>
            {
                if (message.MessageContent == 1)
                {
                    await Task.Delay(1100);
                }

                processedMessages.Add(message);
            });

            var testMessages = Enumerable.Range(0, 10)
                .Select(i => new TestMessage(i))
                .ToArray();

            SetupMockStorageQueue(context, testMessages);

            //Act
            context.CancellationTokenSource.CancelAfter(1500);
            context.SUT.ProcessQueue(onMessageTask, context.CancellationTokenSource.Token).GetAwaiter().GetResult();

            //Assert
            context.MockStorageQueue.Verify(m => m.RenewMessage(
                It.Is<StorageQueueMessage<TestMessage>>(s => s.Message.MessageContent == 1),
                It.Is<TimeSpan>(t => t.TotalSeconds == _visibilityTimeoutInSeconds)
            ));

            context.MockStorageQueue.Verify(m => m.RenewMessage(
                    It.Is<StorageQueueMessage<TestMessage>>(s => s.Message.MessageContent != 1),
                    It.Is<TimeSpan>(t => t.TotalSeconds == _visibilityTimeoutInSeconds)
                ),
                Times.Never
            );
        }

        private void SetupMockStorageQueue(StorageQueueProcessingServiceTestContext context, TestMessage message)
        {
            SetupMockStorageQueue(context, new[] { message });
        }

        private void SetupMockStorageQueue(StorageQueueProcessingServiceTestContext context, TestMessage[] messages)
        {
            var enumerator = messages.GetEnumerator();

            context.MockStorageQueue.Setup(m => m.GetMessage(
                It.Is<TimeSpan>(t => t.TotalSeconds == _visibilityTimeoutInSeconds)
            ))
            .ReturnsAsync(() =>
            {
                if (enumerator.MoveNext())
                {
                    var message = enumerator.Current as TestMessage;
                    return GetStorageQueueMessage(message);
                }

                return null;
            });
        }

        private IStorageQueueMessage<TestMessage> GetStorageQueueMessage(TestMessage message)
        {
            if (message == null)
            {
                return null;
            }

            return new StorageQueueMessage<TestMessage>()
            {
                CloudQueueMessage = message,
                Message = message
            };
        }

        public class TestMessage
        {
            public int MessageContent { get; set; }

            public TestMessage(int content)
            {
                this.MessageContent = content;
            }
        }
    }
}