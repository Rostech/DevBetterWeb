﻿using System;
using System.IO;
using System.Threading.Tasks;
using DevBetterWeb.Core;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DevBetterWeb.Web.Controllers;

[Route(Constants.STRIPE_API_ENDPOINT)]
public class StripeWebhookHandler : Controller
{
  private readonly IAppLogger<StripeWebhookHandler> _logger;
  private readonly IPaymentHandlerSubscription _paymentHandlerSubscription;
  private readonly IPaymentHandlerEventService _paymentHandlerEventService;
  private readonly IPaymentHandlerInvoice _paymentHandlerInvoice;
  private readonly IWebhookHandlerService _webhookHandlerService;

  public StripeWebhookHandler(IAppLogger<StripeWebhookHandler> logger,
    IPaymentHandlerSubscription paymentHandlerSubscription,
    IPaymentHandlerEventService paymentHandlerEvent,
    IPaymentHandlerInvoice paymentHandlerInvoice,
    IWebhookHandlerService webhookHandlerService)
  {
    _logger = logger;
    _paymentHandlerSubscription = paymentHandlerSubscription;
    _paymentHandlerEventService = paymentHandlerEvent;
    _paymentHandlerInvoice = paymentHandlerInvoice;
    _webhookHandlerService = webhookHandlerService;
  }

  [HttpPost]
  public async Task<IActionResult> Index()
  {
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    _logger.LogInformation($"Processing json\n{json}");

    try
    {
      var paymentHandlerEvent = _paymentHandlerEventService.FromJson(json);
      _logger.LogInformation($"Processing Stripe Event Type: {paymentHandlerEvent.EventType}");

      if (paymentHandlerEvent.EventType.Equals(StripeConstants.INVOICE_PAYMENT_SUCCEEDED_EVENT_TYPE))
      {
        await HandleInvoicePaymentSucceeded(json);
      }
      else if (paymentHandlerEvent.EventType.Equals(StripeConstants.CUSTOMER_SUBSCRIPTION_DELETED_EVENT_TYPE))
      {
        await HandleCustomerSubscriptionEnded(json);
      }
      else if (paymentHandlerEvent.EventType.Equals(StripeConstants.CUSTOMER_SUBSCRIPTION_UPDATED_EVENT_TYPE))
      {
        await HandleCustomerSubscriptionUpdatedEvent(json);
      }
      else
      {
        throw new Exception($"Unhandled Stripe event type {paymentHandlerEvent.EventType}");
      }
      return Ok();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Stripe callback error", json);
      throw;
    }
  }

  private async Task HandleInvoicePaymentSucceeded(string json)
  {
    var billingReason = _paymentHandlerInvoice.GetBillingReason(json);

    if (billingReason == StripeConstants.INVOICE_PAYMENT_SUCCEEDED_FOR_SUBSCRIPTION_CREATION)
    {
      await HandleNewCustomerSubscription(json);
    }
    else if (billingReason == StripeConstants.INVOICE_PAYMENT_SUCCEEDED_FOR_SUBSCRIPTION_RENEWAL)
    {
      await HandleCustomerSubscriptionRenewed(json);
    }
  }

  private Task HandleNewCustomerSubscription(string json)
  {
    return _webhookHandlerService.HandleNewCustomerSubscriptionAsync(json);
  }

  private Task HandleCustomerSubscriptionRenewed(string json)
  {
    return _webhookHandlerService.HandleCustomerSubscriptionRenewedAsync(json);
  }

  private Task HandleCustomerSubscriptionEnded(string json)
  {
    return _webhookHandlerService.HandleCustomerSubscriptionEndedAsync(json);
  }

  private async Task HandleCustomerSubscriptionUpdatedEvent(string json)
  {
    var paymentHandlerEvent = _paymentHandlerEventService.FromJson(json);
    var cancelAtPeriodEnd = _paymentHandlerSubscription.GetCancelAtPeriodEnd(paymentHandlerEvent.SubscriptionId);

    if (cancelAtPeriodEnd)
    {
      await HandleCustomerSubscriptionCancelledAtPeriodEnd(json);
    }
  }

  private Task HandleCustomerSubscriptionCancelledAtPeriodEnd(string json)
  {
    return _webhookHandlerService.HandleCustomerSubscriptionCancelledAtPeriodEndAsync(json);
  }
}
