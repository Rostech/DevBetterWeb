﻿using Ardalis.Specification;
using DevBetterWeb.Core.Entities;

namespace DevBetterWeb.Core.Specs;

public sealed class MemberByEmailSpec : Specification<Member>, ISingleResultSpecification
{
  public MemberByEmailSpec(string email)
  {
    Query
	    .Where(member => member.Email == email);
  }
}
