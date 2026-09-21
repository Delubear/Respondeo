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
