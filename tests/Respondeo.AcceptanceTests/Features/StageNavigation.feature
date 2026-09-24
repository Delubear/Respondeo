Feature: Stage navigation
  As a visitor
  I want stage pages and section-aware breadcrumbs to behave consistently
  So that I always know where I am and can navigate directly

  Scenario: Visiting a stage URL directly loads that stage
	Given I open the "why-god" stage directly
	Then I should see at least one stage card

  Scenario: An unknown path shows the not found page
	Given I open the "not-a-real-stage" path directly
	Then I should see the not found page

  Scenario: The breadcrumb is rooted at the stage, not Home
	Given I open the "why-god" stage directly
	When I choose the first branch card
	Then the breadcrumb root should not be "Home"

  Scenario: Using the masthead nav resets the trail
	Given I open the start page
	When I choose the first stage card
	And I choose the first branch card
	And I choose the first masthead stage
	And I choose the first branch card
	Then the breadcrumb should contain 1 step
