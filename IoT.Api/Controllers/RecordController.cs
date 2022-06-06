using AutoMapper;
using IoT.Api.DataObjects;
using IoT.Contracts;
using IoT.Core.Entities;
using IoT.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IoT.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RecordController : ControllerBase
    {
        private readonly IRecordRepository _recordRepository;
        private readonly UserManager _userManager;
        private readonly IMapper _mapper;

        public RecordController(IRecordRepository recordRepository, UserManager userManager, IMapper mapper)
        {
            _recordRepository = recordRepository;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            var records = await _recordRepository.FindAll().ToListAsync(cancellationToken);
            return Ok(_mapper.Map<IEnumerable<RecordDTO>>(records));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken = default)
        {
            var record = await _recordRepository.FindByIdAsync(id, cancellationToken);
            if (record is null)
                return NotFound();

            return Ok(_mapper.Map<RecordDTO>(record));
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetByUserId(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return BadRequest(new { message = "User not found" });

            var records = await _recordRepository.FindByUserId(userId, cancellationToken).ToListAsync();
            if (records is null) return NotFound("No record found with user");

            return Ok(_mapper.Map<IEnumerable<RecordDTO>>(records));
        }


        [HttpGet("{userId}")]
        public async Task<IActionResult> GetLatest(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return BadRequest(new { message = "User not found" });

            var record = await _recordRepository.FindLatest(userId);
            if(record is null) return NotFound("No record found with user");

            return Ok(_mapper.Map<RecordDTO>(record));
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RecordDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null)
                return BadRequest(new { message = "User not found" });

            var record = _mapper.Map<Record>(dto);
            record.DateIssued = DateTime.Now;
            record.User = user;

            _recordRepository.Add(record);
            await _recordRepository.SaveChangesAsync(cancellationToken);

            return Ok();
        }


        [HttpPut]
        public async Task<IActionResult> Update([FromBody] RecordDTO dto, CancellationToken cancellationToken = default)
        {
            var record = await _recordRepository.FindByIdAsync(dto.Id, cancellationToken);
            if (record is null)
                return NotFound();

            _mapper.Map(dto, record);
            _recordRepository.Update(record);
            await _recordRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var record = await _recordRepository.FindByIdAsync(id, cancellationToken);
            if (record is null)
                return NotFound();

            _recordRepository.Delete(record);
            await _recordRepository.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
    }
}
