Feature: Breadcrumb trail
  As a visitor exploring the content graph
  I want the breadcrumb to reflect the path I took
  So that I can retrace and reset my journey

  Scenario: The breadcrumb grows as I go deeper
	Given I open the start page
	When I choose the first stage card
	And I choose the first branch card
	And I choose the first branch card
	Then the breadcrumb should contain at least 2 steps

  Scenario: Returning to Start resets the trail
	Given I open the start page
	When I choose the first stage card
	And I choose the first branch card
	And I navigate back to the start page
	And I choose the first stage card
	And I choose the first branch card
	Then the breadcrumb should contain 1 step
