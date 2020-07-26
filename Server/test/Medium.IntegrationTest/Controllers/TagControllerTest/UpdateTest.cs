﻿using Bogus;
using FluentAssertions;
using Medium.Core.Contracts.V1;
using Medium.Core.Contracts.V1.Request.Tag;
using Medium.Core.Contracts.V1.Response;
using Medium.Core.Contracts.V1.Response.Tag;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Medium.IntegrationTest.Controllers.TagControllerTest
{
    public class UpdateTest : ControllersTest
    {
        private readonly string _requestUri = ApiRoutes.Tags.Update;
        private readonly Guid _requestId = Guid
            .Parse("5d5e9a28-7c3e-4c2a-8098-b866eab33e61");

        private readonly ITestOutputHelper _output;
        private readonly UpdateTagRequest _updateTagRequest;
        private readonly Faker _faker;

        public UpdateTest(CustomWebApplicationFactory factory, 
            ITestOutputHelper output) : base(factory)
        {
            _faker = new Faker("pt_BR");
            _output = output;

            _updateTagRequest = new UpdateTagRequest
            {
                Name = _faker.Random.String2(8)
            };
        }

        [Fact]
        public async Task ShouldBeReturned_NotFoundResponse_IfTagIdNotExists()
        {
            await AuthenticateAsync();

            var randomId = Guid.NewGuid().ToString();

            var response = await HttpClientTest.PutAsJsonAsync(
                _requestUri.Replace("{tagId}", randomId), _updateTagRequest);

            _output.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ShouldBeReturned_ErrorResponse_IfNameIsInvalid()
        {
            await AuthenticateAsync();

            _updateTagRequest.Name = string.Empty;

            var response = await HttpClientTest.PutAsJsonAsync(
                _requestUri.Replace("{tagId}", _requestId.ToString()),
                    _updateTagRequest);

            _output.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();

            JObject jContent = JObject.Parse(content);
            jContent.Value<string>("title").Should()
                .Be("One or more validation errors occurred.");
            jContent.GetValue("errors")["Name"].HasValues.Should()
                .BeTrue();
        }

        [Fact]
        public async Task ShouldBeReturned_SuccessResponse_AndUpdatedTagInDatabase()
        {
            await AuthenticateAsync();

            var response = await HttpClientTest.PutAsJsonAsync(
                _requestUri.Replace("{tagId}", _requestId.ToString()),
                    _updateTagRequest);

            _output.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsAsync<Response<TagResponse>>())
                .Data.Should().BeEquivalentTo(new Response<TagResponse>(
                    new TagResponse
                    {
                        Id = _requestId,
                        Name = _updateTagRequest.Name
                    }), x => x.ExcludingMissingMembers());
        }
    }
}
