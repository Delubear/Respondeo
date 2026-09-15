Feature: Home page
  As a visitor
  I want to land on the start page
  So that I can choose a starting point for my journey

  Scenario: The start page shows entry-point cards
	Given I open the start page
	Then I should see at least one entry-point card

  Scenario: Choosing an entry point navigates to a node
	Given I open the start page
	When I choose the first entry-point card
	Then the page should show a breadcrumb
