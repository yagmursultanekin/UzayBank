using AutoMapper;
using UzayBank.Application.DTOs;
using UzayBank.Domain.Entities;

namespace UzayBank.Application.Mappings;
    public class MappingProfile : Profile
    {
        public MappingProfile()
    {
        CreateMap<Account, AccountDto>();
        CreateMap<Transaction,TransactionDto>();
    }

    }
