﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Bot.Builder.Adapters.Twilio.Tests
{
    public class TwilioHelperTests
    {
        private const string AuthTokenString = "authToken";
        private const string TwilioNumber = "+12345678";
        private readonly Uri _validationUrlString = new Uri("http://contoso.com");
        private readonly HMACSHA1 _hmac = new HMACSHA1(Encoding.UTF8.GetBytes(AuthTokenString));
        private readonly TwilioAdapterOptions _testOptions = new TwilioAdapterOptions("Test", "Test", "Test", new Uri("http://contoso.com"));

        [Fact]
        public void ActivityToTwilioShouldReturnMessageOptionsWithMediaUrl()
        {
            var activity = JsonConvert.DeserializeObject<Activity>(File.ReadAllText(PathUtils.NormalizePath(Directory.GetCurrentDirectory() + @"\files\Activities.json")));
            activity.Attachments = new List<Attachment> { new Attachment(contentUrl: "http://example.com") };
            var messageOption = TwilioHelper.ActivityToTwilio(activity, TwilioNumber);

            Assert.Equal(activity.Conversation.Id, messageOption.ApplicationSid);
            Assert.Equal(TwilioNumber, messageOption.From.ToString());
            Assert.Equal(activity.Text, messageOption.Body);
            Assert.Equal(new Uri(activity.Attachments[0].ContentUrl), messageOption.MediaUrl[0]);
        }

        [Fact]
        public void ActivityToTwilioShouldReturnEmptyMediaUrlWithNullActivityAttachments()
        {
            var activity = new Activity()
            {
                Conversation = new ConversationAccount()
                {
                    Id = "testId",
                },
                Text = "Testing Null Attachments",
                Attachments = null,
            };
            var messageOptions = TwilioHelper.ActivityToTwilio(activity, TwilioNumber);
            
            Assert.True(messageOptions.MediaUrl.Count == 0);
        }

        [Fact]
        public void ActivityToTwilioShouldReturnEmptyMediaUrlWithNullMediaUrls()
        {
            var activity = JsonConvert.DeserializeObject<Activity>(File.ReadAllText(PathUtils.NormalizePath(Directory.GetCurrentDirectory() + @"\files\Activities.json")));
            activity.Attachments = null;
            var messageOption = TwilioHelper.ActivityToTwilio(activity, TwilioNumber);

            Assert.Equal(activity.Conversation.Id, messageOption.ApplicationSid);
            Assert.Equal(TwilioNumber, messageOption.From.ToString());
            Assert.Equal(activity.Text, messageOption.Body);
            Assert.Empty(messageOption.MediaUrl);
        }

        [Fact]
        public void ActivityToTwilioShouldReturnNullWithNullActivity()
        {
            Assert.Null(TwilioHelper.ActivityToTwilio(null, TwilioNumber));
        }

        [Fact]
        public void ActivityToTwilioShouldReturnNullWithEmptyOrInvalidNumber()
        {
            Assert.Null(TwilioHelper.ActivityToTwilio(default, "not_a_number"));
            Assert.Null(TwilioHelper.ActivityToTwilio(default, string.Empty));
        }

        [Fact]
        public void QueryStringToDictionaryShouldReturnDictionaryWithValidQuery()
        {
            var bodyString = File.ReadAllText(PathUtils.NormalizePath(Directory.GetCurrentDirectory() + @"\Files\NoMediaPayload.txt"));
            
            var dictionary = TwilioHelper.QueryStringToDictionary(bodyString);

            Assert.True(dictionary.ContainsKey("MessageSid"));
            Assert.True(dictionary.ContainsKey("From"));
            Assert.True(dictionary.ContainsKey("To"));
            Assert.True(dictionary.ContainsKey("Body"));
        }

        [Fact]
        public void QueryStringToDictionaryShouldReturnEmptyDictionaryWithEmptyQuery()
        {
            var dictionary = TwilioHelper.QueryStringToDictionary(string.Empty);

            Assert.Empty(dictionary);
        }

        [Fact]
        public void PayloadToActivityShouldReturnNullActivityAttachmentsWithNumMediaEqualToZero()
        {
            var payload = File.ReadAllText(PathUtils.NormalizePath(Directory.GetCurrentDirectory() + @"\Files\DictionaryPayload.json"));
            var dictionaryPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);

            var activity = TwilioHelper.PayloadToActivity(dictionaryPayload);

            Assert.Null(activity.Attachments);
        }

        [Fact]
        public void PayloadToActivityShouldReturnActivityAttachmentsWithNumMediaGreaterThanZero()
        {
            var payload = File.ReadAllText(PathUtils.NormalizePath(Directory.GetCurrentDirectory() + @"\Files\DictionaryMediaPayload.json"));
            var dictionaryPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);

            var activity = TwilioHelper.PayloadToActivity(dictionaryPayload);

            Assert.Equal(1, activity.Attachments.Count);
        }

        [Fact]
        public void PayloadToActivityShouldNotThrowKeyNotFoundExceptionWithNumMediaGreaterThanAttachments()
        {
            var payload = File.ReadAllText(PathUtils.NormalizePath(Directory.GetCurrentDirectory() + @"\Files\DictionaryMediaPayload.json"));
            var dictionaryPayload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);
            dictionaryPayload["NumMedia"] = "2";

            var activity = TwilioHelper.PayloadToActivity(dictionaryPayload);

            Assert.Equal(1, activity.Attachments.Count);
        }

        [Fact]
        public void PayloadToActivityShouldReturnNullWithNullBody()
        {
            Assert.Null(TwilioHelper.PayloadToActivity(null));
        }
    }
}
