﻿using EDennis.NetStandard.Base;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using M = IdentityServer4.Models;

namespace EDennis.AspNetIdentityServer {

    /// <summary>
    /// Controller for managing ApiScopes in IdentityServer.  
    /// Note that when ApiScopes are used that the "aud" claim is not generated
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public abstract class IdpApiScopeController<TContext> : IdpBaseController
        where TContext : ConfigurationDbContext {

        private readonly TContext _dbContext;

        public IdpApiScopeController(TContext dbContext) {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Returns an instance of IdentityServer4.Models.ApiScope, whose name
        /// matches the name route parameter
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("{name}")]
        public async Task<IActionResult> GetAsync([FromRoute] string name) {

            var result = await _dbContext.ApiScopes.FirstOrDefaultAsync(a => a.Name == name);
            if (result == null)
                return NotFound();
            else
                return Ok(result.ToModel());
        }


        /// <summary>
        /// Deletes a ApiScope, whose Name
        /// matches the name route parameter
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("{name}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] string name) {
            var result = await _dbContext.ApiScopes.FirstOrDefaultAsync(c => c.Name == name);
            if (result == null)
                return NotFound();
            else {
                _dbContext.Remove(result);
                await _dbContext.SaveChangesAsync();
                return NoContent();
            }
        }

        /// <summary>
        /// Creates a new ApiScope record
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] M.ApiScope model) {

            var client = model.ToEntity();

            try {
                _dbContext.Add(client);
                await _dbContext.SaveChangesAsync();
            } catch (DbUpdateException ex) {
                ModelState.AddModelError("", ex.Message);
                return BadRequest(ModelState);
            }

            return Ok();
        }


        /// <summary>
        /// Patch-updates an ApiResource record with data from the provided partialModel
        /// (JSON body).
        /// </summary>
        /// <param name="partialModel">JSON object with properties to update</param>
        /// <param name="name">The Name of the ApiResource to update</param>
        /// <param name="mergeCollections">for each collection property, whether to merge 
        /// (default=true) or replace (false) provided items with existing items</param>
        /// <returns></returns>
        [HttpPatch("{name}")]
        public async Task<IActionResult> PatchAsync([FromBody] JsonElement partialModel,
            [FromRoute] string name, [FromQuery] bool mergeCollections = true) {

            var existing = _dbContext.ApiScopes.FirstOrDefault(a => a.Name == name);
            if (existing == null)
                return NotFound();

            var model = existing.ToModel();

            model.Patch(partialModel, ModelState, mergeCollections);

            if (ModelState.ErrorCount > 0)
                return BadRequest(ModelState);

            existing = model.ToEntity();

            _dbContext.Entry(existing).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            return Ok();

        }

    }
}