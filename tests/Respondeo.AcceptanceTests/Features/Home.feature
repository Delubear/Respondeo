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
	And the "walk onward" control should be visible

  Scenario: Walking the reel reveals later stages
	Given I open the start page
	When I press the "walk onward" control
	Then the "earlier steps" control should be visible

  Scenario: Scrolling the wheel over the reel advances one stage
	Given I open the start page
	When I scroll the wheel forward over the reel
	Then the second stage card should be centered

  Scenario: The hint controls collapse to icon-only on a narrow viewport
	Given I am viewing on a 700 pixel wide screen
	And I open the start page
	Then the "walk onward" control label should be hidden

  Scenario: The off-ramp offers two further paths
	Given I open the start page
	Then I should see the "Already further along the path?" off-ramp
	And I should see an off-ramp pill linking to "discover"
	And I should see an off-ramp pill linking to "summa"

  Scenario: The off-ramp pills are the same width
	Given I open the start page
	Then the off-ramp pills should have the same width

  Scenario: Choosing the Discover off-ramp opens Discover Catholicism
	Given I open the start page
	When I choose the off-ramp pill linking to "discover"
	Then the address should be "/discover"

  Scenario: The feedback link opens a choice of two routes
	Given I open the start page
	When I open the feedback dialog
	Then I should see a feedback choice to open a GitHub issue
	And I should see an anonymous feedback choice that needs no account

  Scenario: The anonymous feedback route needs no sign-in
	Given I open the start page
	When I open the feedback dialog
	Then the anonymous feedback choice should not link to GitHub
