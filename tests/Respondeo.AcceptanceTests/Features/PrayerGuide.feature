Feature: Prayer Guide and inline prayer references
  As a visitor learning to pray
  I want a short orientation to prayer notation and to see referenced prayers in full
  So that I can pray litanies and novenas with confidence

  Scenario: Opening the Guide to Prayers from the prayers browser
	Given I open the prayers browser
	When I open the prayer guide
	Then the prayer guide dialog should be visible
	And the prayer guide dialog should explain the versicle and response

  Scenario: Closing the Guide to Prayers
	Given I open the prayers browser
	When I open the prayer guide
	And I close the prayer guide
	Then the prayer guide dialog should not be visible

  Scenario: A novena shows its referenced prayers inline
	Given I open the "discover/prayers/novena-sacred-heart" prayer directly
	Then the prayer should show at least one embedded prayer
