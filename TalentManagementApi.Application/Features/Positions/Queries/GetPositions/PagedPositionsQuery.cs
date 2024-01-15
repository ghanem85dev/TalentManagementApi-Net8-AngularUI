﻿using AutoMapper;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TalentManagementApi.Application.Interfaces;
using TalentManagementApi.Application.Interfaces.Repositories;
using TalentManagementApi.Application.Parameters;
using TalentManagementApi.Application.Wrappers;
using TalentManagementApi.Domain.Entities;

namespace TalentManagementApi.Application.Features.Positions.Queries.GetPositions
{
    public partial class PagedPositionsQuery : IRequest<PagedDataTableResponse<IEnumerable<Entity>>>
    {
        //strong type input parameters
        public int Draw { get; set; } //page number

        public int Start { get; set; } //Paging first record indicator. This is the start point in the current data set (0 index based - i.e. 0 is the first record).
        public int Length { get; set; } //page size
        public IList<SortOrder> Order { get; set; } //Order by
        public Search Search { get; set; } //search criteria
        public IList<Column> Columns { get; set; } //select fields
    }

    public class PagePositionQueryHandler : IRequestHandler<PagedPositionsQuery, PagedDataTableResponse<IEnumerable<Entity>>>
    {
        private readonly IPositionRepositoryAsync _repository;
        private readonly IModelHelper _modelHelper;

        public PagePositionQueryHandler(IPositionRepositoryAsync repository, IMapper mapper, IModelHelper modelHelper)
        {
            _repository = repository;
            _modelHelper = modelHelper;
        }

        public async Task<PagedDataTableResponse<IEnumerable<Entity>>> Handle(PagedPositionsQuery request, CancellationToken cancellationToken)
        {
            var validatedRequest = new GetPositionsQuery();

            // Draw map to PageNumber
            validatedRequest.PageNumber = (request.Start / request.Length) + 1;
            // Length map to PageSize
            validatedRequest.PageSize = request.Length;

            // Map order > OrderBy
            var colOrder = request.Order[0];
            switch (colOrder.Column)
            {
                case 0:
                    validatedRequest.OrderBy = colOrder.Dir == "asc" ? "PositionNumber" : "PositionNumber DESC";
                    break;

                case 1:
                    validatedRequest.OrderBy = colOrder.Dir == "asc" ? "PositionTitle" : "PositionTitle DESC";
                    break;

                case 2:
                    validatedRequest.OrderBy = colOrder.Dir == "asc" ? "Department.Name" : "Department.Name DESC";
                    break;
            }

            // Map Search > searchable columns
            var keywordSearch = request.Search.Value;

            if (!string.IsNullOrEmpty(keywordSearch))
            {
                //limit to fields in view model
                validatedRequest.PositionNumber = keywordSearch;
                validatedRequest.PositionTitle = keywordSearch;
                validatedRequest.Department = keywordSearch;
            }
            if (string.IsNullOrEmpty(validatedRequest.Fields))
            {
                //default fields from view model
                validatedRequest.Fields = _modelHelper.GetModelFields<GetPositionsViewModel>();
            }
            // query based on filter
            var qryResult = await _repository.GetPagedPositionReponseAsync(validatedRequest);
            var data = qryResult.data;
            RecordsCount recordCount = qryResult.recordsCount;

            // response wrapper
            return new PagedDataTableResponse<IEnumerable<Entity>>(data, request.Draw, recordCount);
        }
    }
}