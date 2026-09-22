Feature: Home page
  As a visitor
  I want to land on the start page
  So that I can choose a starting point for my journey

  Scenario: The start page shows stage cards
	Given I open the start page
	Then I should see at least one stage card

  Scenario: Choosing a stage and branch navigates to a node
	Given I open the start page
	When I choose the first stage card
	And I choose the first branch card
	Then the page should show a breadcrumb

  Scenario: The reel starts at the beginning of the journey
	Given I open the start page
	Then the "earlier steps" control should be hidden
	And the "keep climbing" control should be visible

  Scenario: Climbing the reel reveals later stages
	Given I open the start page
	When I press the "keep climbing" control
	Then the "earlier steps" control should be visible
