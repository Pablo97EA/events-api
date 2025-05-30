using Xunit;
using api.Controllers;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using api.Dtos.Event;
using System;
using System.Linq;

public class EventControllerTests
{
    private ApplicationDBContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }

    [Fact]
    public async Task Create_Returns_CreatedAtActionResult()
    {
        var context = GetInMemoryDbContext();
        var controller = new EventController(context);

        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedImages");
        Directory.CreateDirectory(imagePath);

        var fileMock = new FormFile(
            baseStream: new MemoryStream(new byte[10]),
            baseStreamOffset: 0,
            length: 10,
            name: "Data",
            fileName: "1.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var newEvent = new CreateEventRequestDto
        {
            Name = "Test Event",
            Description = "Test Description",
            Location = "Test Location",
            Date = DateTime.Now,
            File = fileMock
        };

        var result = await controller.Create(newEvent);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsAllEvents()
    {
        var context = GetInMemoryDbContext();
        context.Events.Add(new Event { Name = "Event1", Description = "Desc1", Location = "Loc1", Date = DateTime.Now });
        context.Events.Add(new Event { Name = "Event2", Description = "Desc2", Location = "Loc2", Date = DateTime.Now });
        await context.SaveChangesAsync();

        var controller = new EventController(context);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventDto>>(okResult.Value);
        Assert.Equal(2, events.Count());
    }

    [Fact]
    public async Task GetById_ReturnsEvent_WhenFound()
    {
        var context = GetInMemoryDbContext();
        var ev = new Event { Name = "Event1", Description = "Desc1", Location = "Loc1", Date = DateTime.Now };
        context.Events.Add(ev);
        await context.SaveChangesAsync();

        var controller = new EventController(context);

        var result = await controller.getById(ev.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var eventDto = Assert.IsType<EventDto>(okResult.Value);
        Assert.Equal(ev.Name, eventDto.Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNotFound()
    {
        var context = GetInMemoryDbContext();
        var controller = new EventController(context);

        var result = await controller.getById(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenEventUpdated()
    {
        var context = GetInMemoryDbContext();
        var ev = new Event { Name = "OldName", Description = "OldDesc", Location = "OldLoc", Date = DateTime.Now };
        context.Events.Add(ev);
        await context.SaveChangesAsync();

        var controller = new EventController(context);

        var updateDto = new UpdateEventRequestDto
        {
            Name = "NewName",
            Description = "NewDesc",
            Location = "NewLoc",
            Date = DateTime.Now.AddDays(1)
        };

        var result = await controller.Update(ev.Id, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedEvent = Assert.IsType<EventDto>(okResult.Value);
        Assert.Equal("NewName", updatedEvent.Name);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenEventDoesNotExist()
    {
        var context = GetInMemoryDbContext();
        var controller = new EventController(context);

        var updateDto = new UpdateEventRequestDto
        {
            Name = "NewName",
            Description = "NewDesc",
            Location = "NewLoc",
            Date = DateTime.Now.AddDays(1)
        };

        var result = await controller.Update(999, updateDto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenEventDeleted()
    {
        var context = GetInMemoryDbContext();
        var ev = new Event { Name = "ToDelete", Description = "Desc", Location = "Loc", Date = DateTime.Now };
        context.Events.Add(ev);
        await context.SaveChangesAsync();

        var controller = new EventController(context);

        var result = await controller.Delete(ev.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await context.Events.FindAsync(ev.Id));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenEventNotExist()
    {
        var context = GetInMemoryDbContext();
        var controller = new EventController(context);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
